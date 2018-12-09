using Automatonymous;
using CQRSlite.Domain;
using CQRSlite.Domain.Exception;
using CQRSlite.Events;
using FluentAssertions;
using GreenPipes;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Scoping;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Sds.CqrsLite.MassTransit.Filters;
using Sds.MassTransit.Extensions;
using Sds.Osdr.Domain.AccessControl;
using Sds.Osdr.Domain.BddTests.Extensions;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Osdr.MachineLearning.Sagas.Events;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using Sds.Storage.KeyValue.Core;
using Sds.Storage.KeyValue.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sds.Osdr.BddTests
{
    public class OsdrTestHarness : IDisposable
    {
        private BusTestHarness _harness;
        protected IServiceProvider _serviceProvider;
        public Guid JohnId { get; private set; }
        public Guid JaneId { get; private set; }

        public ISession Session { get { return new Session(_serviceProvider.GetService<IRepository>()); } }
        public IBlobStorage BlobStorage { get { return _serviceProvider.GetService<IBlobStorage>(); } }
        public IEventStore CqrsEventStore { get { return _serviceProvider.GetService<IEventStore>(); } }
        public EventStore.IEventStore EventStore { get { return _serviceProvider.GetService<EventStore.IEventStore>(); } }
        public IMongoDatabase MongoDb { get { return _serviceProvider.GetService<IMongoDatabase>(); } }

        public IBusControl BusControl { get { return _serviceProvider.GetService<IBusControl>(); } }
        public BusTestHarness Harness { get { return _harness; } }

        private string MongoDatabaseName { get { return $"osdr_test_{JohnId}"; } }

        private IDictionary<Guid, IList<Guid>> ProcessedRecords { get; } = new Dictionary<Guid, IList<Guid>>();
        private IDictionary<Guid, IList<Guid>> InvalidRecords { get; } = new Dictionary<Guid, IList<Guid>>();
        private IDictionary<Guid, int> PersistedRecords { get; } = new Dictionary<Guid, int>();
        private IDictionary<Guid, IList<Guid>> DependentFiles { get; } = new Dictionary<Guid, IList<Guid>>();

        private List<ExceptionInfo> Faults = new List<ExceptionInfo>();

        public OsdrTestHarness()
		{
            var builder = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.bddtests.json", true, true);

            var configuration = builder.Build();

            var services = new ServiceCollection();

            JohnId = NewId.NextGuid();
		    JaneId = NewId.NextGuid();

            var testHarnessSettings = configuration.GetSection("TestHarness").Get<TestHarnessSettings>();

            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(testHarnessSettings.Timeout);

            services.AddSingleton<IBlobStorage, InMemoryStorage>();

            services.AddOptions();

            services.AddSingleton<EventStore.InMemoryEventStore>();
            services.AddSingleton<IEventStore>(c => c.GetService<EventStore.InMemoryEventStore>());
            services.AddSingleton<EventStore.IEventStore>(c => c.GetService<EventStore.InMemoryEventStore>());
            services.AddScoped<IEventPublisher, CqrsLite.MassTransit.MassTransitEventPublisher>();
            services.AddScoped<ISession, Session>();
            services.AddScoped<IRepository, Repository>();
            services.AddScoped<IKeyValueRepository, InMemoryKeyValueRepository>();

            services.AddSingleton<IConsumerScopeProvider, DependencyInjectionConsumerScopeProvider>();

            services.Configure<Infrastructure.AccessControl.AccessControl>(configuration.GetSection("AccessControl"));
            services.AddSingleton<IAccessControl, Infrastructure.AccessControl.AppSettingsAccessControl>();

            services.AddSingleton(new MongoClient(Environment.ExpandEnvironmentVariables("%OSDR_MONGO_DB%")));
            services.AddScoped(service => service.GetService<MongoClient>().GetDatabase(MongoDatabaseName));

			var allAssembly = new Assembly[]
            {
                Assembly.LoadFrom("Sds.Osdr.Domain.BddTests.dll"),
                //Assembly.LoadFrom("Sds.Osdr.Domain.BackEnd.dll")
            };

            services.AddAllConsumers(allAssembly);

            services.AddScoped<Domain.BackEnd.EventHandlers.MachineLearningEventHandlers>();
            services.AddScoped<Domain.BackEnd.EventHandlers.MicroServiceEventHandlers>();

            var moduleAssemblies = new Assembly[]
            {
                Assembly.LoadFrom("Sds.Osdr.Generic.dll"),
                Assembly.LoadFrom("Sds.Osdr.RecordsFile.dll"),
                Assembly.LoadFrom("Sds.Osdr.Chemicals.dll"),
                Assembly.LoadFrom("Sds.Osdr.Crystals.dll"),
                Assembly.LoadFrom("Sds.Osdr.Reactions.dll"),
                Assembly.LoadFrom("Sds.Osdr.Spectra.dll"),
                Assembly.LoadFrom("Sds.Osdr.Pdf.dll"),
                Assembly.LoadFrom("Sds.Osdr.Images.dll"),
                Assembly.LoadFrom("Sds.Osdr.Office.dll"),
                Assembly.LoadFrom("Sds.Osdr.Tabular.dll"),
                Assembly.LoadFrom("Sds.Osdr.MachineLearning.dll"),
                Assembly.LoadFrom("Sds.Osdr.WebPage.dll"),
            };

            services.UseInMemoryOsdrModules(moduleAssemblies);

            services.AddSingleton((ctx) =>
            {
                return _harness.Bus as IBusControl;
            });

            _harness.OnConfigureBus += cfg =>
            {
                cfg.UseInMemoryOutbox();

                cfg.RegisterScopedConsumer<Domain.BackEnd.EventHandlers.MachineLearningEventHandlers>(_serviceProvider, null, c => c.UseCqrsLite());
                cfg.RegisterScopedConsumer<Domain.BackEnd.EventHandlers.MicroServiceEventHandlers>(_serviceProvider, null, c => c.UseCqrsLite());

                cfg.RegisterInMemoryOsdrModules(_serviceProvider, moduleAssemblies);

                cfg.RegisterConsumers(_serviceProvider, allAssembly);

                cfg.UseRetry(r =>
                {
                    r.Interval(100, TimeSpan.FromMilliseconds(10));
                    r.Handle<MongoWriteException>();
                    r.Handle<UnhandledEventException>();
                    r.Handle<ConcurrencyException>();
                });
            };

            _harness.Handler<RecordsFile.Sagas.Events.RecordProcessed>(async context =>
            {
                lock(ProcessedRecords)
                {
                    var recordId = context.Message.Id;
                    var fileId = context.Message.FileId;

                    if (!ProcessedRecords.ContainsKey(fileId))
                    {
                        ProcessedRecords[fileId] = new List<Guid>();
                    }

                    ProcessedRecords[fileId].Add(recordId);
                }

                await Task.CompletedTask;
            });
		    
            _harness.Handler<Generic.Sagas.Events.FileProcessed>(async context =>
            {
                lock(ProcessedRecords)
                {
                    var recordId = context.Message.Id;
                    var parentId = context.Message.ParentId;

                    if (!DependentFiles.ContainsKey(parentId))
                    {
                        DependentFiles[parentId] = new List<Guid>();
                    }

                    DependentFiles[parentId].Add(recordId);
                }

                await Task.CompletedTask;
            });

            _harness.Handler<ModelTrainingFinished>(async context =>
            {
                lock (ProcessedRecords)
                {
                    var recordId = context.Message.Id;
                    var parentId = context.Message.ParentId;

                    if (!DependentFiles.ContainsKey(parentId))
                    {
                        DependentFiles[parentId] = new List<Guid>();
                    }

                    DependentFiles[parentId].Add(recordId);
                }

                await Task.CompletedTask;
            });

            _harness.Handler<RecordsFile.Sagas.Events.InvalidRecordProcessed>(async context =>
            {
                lock (InvalidRecords)
                {
                    var recordId = context.Message.Id;
                    var fileId = context.Message.FileId;

                    if (!InvalidRecords.ContainsKey(fileId))
                    {
                        InvalidRecords[fileId] = new List<Guid>();
                    }

                    InvalidRecords[fileId].Add(recordId);
                }

                await Task.CompletedTask;
            });

            _harness.Handler<RecordsFile.Domain.Events.Records.StatusPersisted>(async context =>
            {
                if (context.Message.Status == RecordsFile.Domain.RecordStatus.Processed)
                {
                    lock (PersistedRecords)
                    {
                        if (!PersistedRecords.ContainsKey(context.Message.Id))
                        {
                            PersistedRecords[context.Message.Id] = 1;
                        }
                        else
                        {
                            PersistedRecords[context.Message.Id] = PersistedRecords[context.Message.Id] + 1;
                        }
                    }
                }

                await Task.CompletedTask;
            });

            _harness.Handler<Fault>(async context =>
            {
                Faults.AddRange(context.Message.Exceptions.Where(ex => !ex.ExceptionType.Equals("System.InvalidOperationException")));

                await Task.CompletedTask;
            });

            _serviceProvider = services.BuildServiceProvider();

            _harness.Start().Wait();

            Seed(JohnId, "John Doe", "John", "Doe", "john", "john@your-company.com", null).Wait();
            Seed(JaneId, "Jane Doe", "Jane", "Doe", "jane", "jane@your-company.com", null).Wait();

            //  Specify how to compare DateTimes inside FluentAssertions
            AssertionOptions.AssertEquivalencyUsing(options =>
              options
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, (int)_harness.TestTimeout.TotalMilliseconds)).WhenTypeIs<DateTime>()
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, (int)_harness.TestTimeout.TotalMilliseconds)).WhenTypeIs<DateTimeOffset>()
            );
        }
        
        private async Task Seed(Guid userId, string displayName, string firstName, string lastName, string loginName, string email, string avatar)
        {
//            await Harness.CreateUser(User1Id, "John Doe", "John", "Doe", "john", "john@your-company.com", null, User1Id);
            await Harness.CreateUser(userId, displayName, firstName, lastName, loginName, email, avatar, userId);
        }

        public IEnumerable<ExceptionInfo> GetFaults()
        {
            return Faults;
        }

        public IEnumerable<Guid> GetProcessedRecords(Guid fileId)
        {
            if (ProcessedRecords.ContainsKey(fileId))
            {
                return ProcessedRecords[fileId];
            }

            return new Guid[] { };
        }

        public IEnumerable<Guid> GetDependentFiles(Guid parentId)
        {
            if (DependentFiles.ContainsKey(parentId))
            {
                return DependentFiles[parentId];
            }

            return new Guid[] { };
        }

        //public async Task<IEnumerable<Guid>> GetDependentFiles(Guid parentId, FileType type)
        //{
        //    List<Guid> files = new List<Guid>();

        //    if (DependentFiles.ContainsKey(parentId))
        //    {
        //        foreach(var id in DependentFiles[parentId])
        //        {
        //            var file = await Session.Get<File>(id);
        //            if (file != null && file.Type == type)
        //            {
        //                files.Add(id);
        //            }
        //        };
        //    }

        //    return files;
        //}

        public async Task<IEnumerable<Guid>> GetDependentFiles(Guid parentId, params FileType[] types)
        {
            List<Guid> files = new List<Guid>();

            if (DependentFiles.ContainsKey(parentId))
            {
                foreach (var id in DependentFiles[parentId])
                {
                    var file = await Session.Get<File>(id);
                    if (file != null && types.Contains(file.Type))
                    {
                        files.Add(id);
                    }
                };
            }

            return files;
        }

        public async Task<IEnumerable<Guid>> GetDependentFilesExcept(Guid parentId, params FileType[] types)
        {
            List<Guid> files = new List<Guid>();

            if (DependentFiles.ContainsKey(parentId))
            {
                foreach (var id in DependentFiles[parentId])
                {
                    var file = await Session.Get<File>(id);
                    if (file != null && !types.Contains(file.Type))
                    {
                        files.Add(id);
                    }
                };
            }

            return files;
        }

        public IEnumerable<Guid> GetInvalidRecords(Guid fileId)
        {
            if (InvalidRecords.ContainsKey(fileId))
            {
                return InvalidRecords[fileId];
            }

            return new Guid[] { };
        }

        public virtual void Dispose()
        {
            _harness.Stop().Wait();

            MongoDb.Client.DropDatabase(MongoDatabaseName);
        }
    }
}
