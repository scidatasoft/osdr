using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Nest;
using Sds.MassTransit.Extensions;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.Indexing.Tests
{
    public class IndexingFixture : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private BusTestHarness _harness;
        public IBlobStorage BlobStorage = new InMemoryStorage();

        public BusTestHarness Harness { get { return _harness; } }
        public IList<IIndexRequest<object>> FakeIndex;
        public Guid UserId { get; private set; }

        public IMongoDatabase MongoDb { get { return _serviceProvider.GetService<IMongoDatabase>(); } }

        private string MongoDatabaseName { get { return $"indexing_test_{UserId}"; } }
        public Mock<IElasticClient> ElasticClientMock;

        public IndexingFixture()
        {
            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(30);

            FakeIndex = new List<IIndexRequest<dynamic>>();
           
            ElasticClientMock = new Mock<IElasticClient>();
            ElasticClientMock
                .Setup(m => m.IndexAsync<object>(It.IsAny<IIndexRequest>(), null, default(CancellationToken)))
                .Returns(Task.FromResult(new Mock<IIndexResponse>().Object))
                .Callback<IIndexRequest<object>, Func<IndexDescriptor<object>, IIndexRequest>, CancellationToken>((a, s, c) => {
                    FakeIndex.Add(a);
                    });

            //ElasticClientMock
            //    .Setup(m => m.UpdateAsync<object, object>(It.Is<IUpdateRequest<object, object>>(r =>
            //        r.DocAsUpsert == true && r.Index.Name == "files" && r.Type.Name == "file"), default(CancellationToken)))
            //    .Returns(Task.FromResult(new Mock<IUpdateResponse<object>>().Object))
            //    .Callback<IIndexRequest<object>, Func<IndexDescriptor<object>, IIndexRequest>, CancellationToken>((a, s, c) =>
            //        FakeIndex.Add(a));

            var services = new ServiceCollection();

            UserId = Guid.NewGuid();
            services.AddTransient(x => ElasticClientMock.Object);

            var allAssemblies = new Assembly[]
            {
                Assembly.Load(new AssemblyName("Sds.Indexing"))
            };

            services.AddAllConsumers(allAssemblies);

            services.AddSingleton((ctx) =>
            {
                return _harness.Bus as IBusControl;
            });

            _harness.OnConfigureBus += cfg =>
            {
                cfg.RegisterConsumers(_serviceProvider, allAssemblies);
            };

            services.AddSingleton(new MongoClient(Environment.ExpandEnvironmentVariables("%OSDR_MONGO_DB%")));
            services.AddScoped(service => service.GetService<MongoClient>().GetDatabase(MongoDatabaseName));
            services.AddTransient(x => BlobStorage);

            _serviceProvider = services.BuildServiceProvider();

            _harness.Start().Wait();
        }

        public void Dispose()
        {
            MongoDb.Client.DropDatabase(MongoDatabaseName);
            _harness.Dispose();
        }
    }
}
