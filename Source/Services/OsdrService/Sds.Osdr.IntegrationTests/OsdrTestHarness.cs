using CQRSlite.Domain;
using CQRSlite.Domain.Exception;
using CQRSlite.Events;
using FluentAssertions;
using GreenPipes;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.RabbitMqTransport;
using MassTransit.Scoping;
using MassTransit.Testing;
using MassTransit.Testing.MessageObservers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Sds.CqrsLite.EventStore;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.Observers;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Settings;
using Sds.Osdr.Generic.Domain;
using Sds.Serilog;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.GridFs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests
{
    public class OsdrTestHarness : IDisposable
    {
        protected IServiceProvider _serviceProvider;
        public Guid JohnId { get; protected set; }
        public Guid JaneId { get; protected set; }

        public ISession Session { get { return new Session(_serviceProvider.GetService<IRepository>()); } }
        public IBlobStorage BlobStorage { get { return _serviceProvider.GetService<IBlobStorage>(); } }
        public IEventStore CqrsEventStore { get { return _serviceProvider.GetService<IEventStore>(); } }
        public EventStore.IEventStore EventStore { get { return _serviceProvider.GetService<EventStore.IEventStore>(); } }
        public IMongoDatabase MongoDb { get { return _serviceProvider.GetService<IMongoDatabase>(); } }

        public IBusControl BusControl { get { return _serviceProvider.GetService<IBusControl>(); } }

        private IDictionary<Guid, IList<Guid>> ProcessedRecords { get; } = new Dictionary<Guid, IList<Guid>>();
        private IDictionary<Guid, IList<Guid>> InvalidRecords { get; } = new Dictionary<Guid, IList<Guid>>();
        private IDictionary<Guid, int> PersistedRecords { get; } = new Dictionary<Guid, int>();
        private IDictionary<Guid, IList<Guid>> DependentFiles { get; } = new Dictionary<Guid, IList<Guid>>();

        private List<ExceptionInfo> Faults = new List<ExceptionInfo>();

        public ReceivedMessageList Received { get; } = new ReceivedMessageList(TimeSpan.FromSeconds(10));

        public OsdrTestHarness()
        {
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", true, true)
                 .AddEnvironmentVariables()
                 .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .MinimumLevel.ControlledBy(new EnvironmentVariableLoggingLevelSwitch("%OSDR_LOG_LEVEL%"))
                .CreateLogger();

            Log.Information("Staring integration tests");

            var services = new ServiceCollection();

            services.AddOptions();
            services.Configure<MassTransitSettings>(configuration.GetSection("MassTransit"));

            services.AddSingleton<IEventStore>(y => new GetEventStore(Environment.ExpandEnvironmentVariables(configuration["EventStore:ConnectionString"])));
            services.AddSingleton<IEventPublisher, CqrsLite.MassTransit.MassTransitBus>();
            services.AddTransient<ISession, Session>();
            services.AddSingleton<IRepository, Repository>();
            services.AddSingleton<EventStore.IEventStore>(new EventStore.EventStore(Environment.ExpandEnvironmentVariables(configuration["EventStore:ConnectionString"])));

            var mongoUrl = new MongoUrl(Environment.ExpandEnvironmentVariables(configuration["MongoDb:ConnectionString"]));

            services.AddSingleton(new MongoClient(mongoUrl));
            services.AddScoped(service => service.GetService<MongoClient>().GetDatabase(mongoUrl.DatabaseName));

            services.AddTransient<IBlobStorage, GridFsStorage>(x =>
            {
                var blobStorageUrl = new MongoUrl(Environment.ExpandEnvironmentVariables(configuration["GridFs:ConnectionString"]));
                var client = new MongoClient(blobStorageUrl);

                return new GridFsStorage(client.GetDatabase(blobStorageUrl.DatabaseName));
            });

            services.AddSingleton<IConsumerScopeProvider, DependencyInjectionConsumerScopeProvider>();

            var integrationTestsAssembly = Assembly.LoadFrom("Sds.Osdr.IntegrationTests.dll");

            services.AddAllConsumers(integrationTestsAssembly);

            services.AddSingleton(container => Bus.Factory.CreateUsingRabbitMq(x =>
            {
                var mtSettings = container.GetService<IOptions<MassTransitSettings>>().Value;

                IRabbitMqHost host = x.Host(new Uri(Environment.ExpandEnvironmentVariables(mtSettings.ConnectionString)), h => { });

                x.RegisterConsumers(host, container, e =>
                {
                    e.UseDelayedRedelivery(r =>
                    {
                        r.Interval(mtSettings.RedeliveryCount, TimeSpan.FromMilliseconds(mtSettings.RedeliveryInterval));
                        r.Handle<ConcurrencyException>();
                    });
                    e.UseMessageRetry(r =>
                    {
                        r.Interval(mtSettings.RetryCount, TimeSpan.FromMilliseconds(mtSettings.RetryInterval));
                        r.Handle<ConcurrencyException>();
                    });

                    e.PrefetchCount = mtSettings.PrefetchCount;
                    e.UseInMemoryOutbox();
                }, integrationTestsAssembly);

                x.ReceiveEndpoint(host, "processing_fault_queue", e =>
                {
                    e.Handler<Fault>(async context =>
                    {
                        Faults.AddRange(context.Message.Exceptions.Where(ex => !ex.ExceptionType.Equals("System.InvalidOperationException")));

                        await Task.CompletedTask;
                    });
                });

                x.ReceiveEndpoint(host, "processing_update_queue", e =>
                {
                    e.Handler<Generic.Domain.Events.Users.UserPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Nodes.UserPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Folders.FolderPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Nodes.FolderPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Files.FileCreated>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Files.NodeStatusPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Files.StatusPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Sagas.Events.FileProcessed>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<RecordsFile.Sagas.Events.RecordsFileProcessed>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<RecordsFile.Sagas.Events.RecordProcessed>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<RecordsFile.Sagas.Events.InvalidRecordProcessed>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<RecordsFile.Domain.Events.Records.NodeStatusPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<RecordsFile.Domain.Events.Records.StatusPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<RecordsFile.Domain.Events.Records.RecordPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<RecordsFile.Domain.Events.Records.NodeRecordPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<MachineLearning.Domain.Events.TrainingFinished>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<MachineLearning.Sagas.Events.ModelTrainingFinished>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<MachineLearning.Sagas.Events.PropertiesPredictionFinished>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<MachineLearning.Domain.Events.ModelPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<MachineLearning.Domain.Events.PermissionChangedPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Folders.DeletedFolderPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Nodes.DeletedFolderPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Folders.MovedFolderPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Nodes.MovedFolderPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Folders.RenamedFolderPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Nodes.RenamedFolderPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Nodes.RenamedFolderPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Nodes.PermissionChangedPersisted>(context => { Received.Add(context); return Task.CompletedTask; });

                    e.Handler<Generic.Domain.Events.Files.PermissionChangedPersisted>(context => { Received.Add(context); return Task.CompletedTask; });
                });

                x.UseConcurrencyLimit(mtSettings.ConcurrencyLimit);
            }));

            _serviceProvider = services.BuildServiceProvider();

            var busControl = _serviceProvider.GetRequiredService<IBusControl>();

            busControl.ConnectPublishObserver(new PublishObserver());
            busControl.ConnectConsumeObserver(new ConsumeObserver());

            busControl.Start();

            Seed();

            //  Specify how to compare DateTimes inside FluentAssertions
            AssertionOptions.AssertEquivalencyUsing(options =>
              options
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 10000)).WhenTypeIs<DateTime>()
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 10000)).WhenTypeIs<DateTimeOffset>()
            );
        }

        protected virtual void Seed()
        {
            JohnId = this.CreateUser("John Doe", "John", "Doe", "john", "john@your-company.com", null, NewId.NextGuid()).Result;
            JaneId = this.CreateUser("Jane Doe", "Jane", "Doe", "jane", "jane@your-company.com", null, NewId.NextGuid()).Result;
        }

        public IEnumerable<ExceptionInfo> GetFaults()
        {
            return Faults;
        }

        public IEnumerable<Guid> GetProcessedRecords(Guid fileId)
        {
            return Received
                .Where(m => m.Context.GetType().IsGenericType && m.Context.GetType().GetGenericArguments()[0] == typeof(RecordsFile.Sagas.Events.RecordProcessed))
                .Select(m => (m.Context as ConsumeContext<RecordsFile.Sagas.Events.RecordProcessed>).Message)
                .Where(m => m.FileId == fileId)
                .Select(m => m.Id)
                .ToList();
        }

        public IEnumerable<Guid> GetInvalidRecords(Guid fileId)
        {
            return Received
                .Where(m => m.Context.GetType().IsGenericType && m.Context.GetType().GetGenericArguments()[0] == typeof(RecordsFile.Sagas.Events.InvalidRecordProcessed))
                .Select(m => (m.Context as ConsumeContext<RecordsFile.Sagas.Events.InvalidRecordProcessed>).Message)
                .Where(m => m.FileId == fileId)
                .Select(m => m.Id)
                .ToList();
        }

        public IEnumerable<Guid> GetDependentFiles(Guid parentId)
        {
            var trainedModels = Received
                .Where(m => m.Context.GetType().IsGenericType && m.Context.GetType().GetGenericArguments()[0] == typeof(MachineLearning.Sagas.Events.ModelTrainingFinished))
                .Select(m => (m.Context as ConsumeContext<MachineLearning.Sagas.Events.ModelTrainingFinished>).Message)
                .Where(m => m.ParentId == parentId)
                .Select(m => m.Id);

            var processedFiles = Received
                .Where(m => m.Context.GetType().IsGenericType && m.Context.GetType().GetGenericArguments()[0] == typeof(Generic.Sagas.Events.FileProcessed))
                .Select(m => (m.Context as ConsumeContext<Generic.Sagas.Events.FileProcessed>).Message)
                .Where(m => m.ParentId == parentId)
                .Select(m => m.Id);

            return trainedModels.Concat(processedFiles);
        }

        public IEnumerable<Guid> GetDependentFiles(Guid parentId, params FileType[] types)
        {
            return GetDependentFiles(parentId)
                .Select(id => Session.Get<File>(id).Result)
                .Where(f => types.Contains(f.Type))
                .Select(f => f.Id);
        }

        public IEnumerable<Guid> GetDependentFilesExcept(Guid parentId, params FileType[] types)
        {
            return GetDependentFiles(parentId)
                .Select(id => Session.Get<File>(id).Result)
                .Where(f => !types.Contains(f.Type))
                .Select(f => f.Id);
        }

        public virtual void Dispose()
        {
            var busControl = _serviceProvider.GetRequiredService<IBusControl>();
            busControl.Stop();
        }
    }
}
