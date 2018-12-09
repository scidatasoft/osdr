using CQRSlite.Domain;
using MassTransit;
using MassTransit.RabbitMqTransport;
using MassTransit.Saga;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Sds.CqrsLite.MassTransit.Filters;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Saga;
using Sds.Osdr.Domain.Modules;
using Sds.Osdr.Infrastructure.Extensions;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using Sds.Osdr.MachineLearning.Sagas;
using Sds.Storage.Blob.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.Modules
{
    public class MachineLearningModule : IModule
    {
        private readonly ISession _session;
        private readonly IBusControl _bus;

        public MachineLearningModule(ISession session, IBusControl bus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public bool IsSupported(BlobLoaded blob)
        {
            if (blob.BlobInfo.Metadata.ContainsKey("FileType"))
            {
                return blob.BlobInfo.Metadata["FileType"].ToString() == "MachineLearningModel";
            }

            return false;
        }

        public async Task Process(BlobLoaded blob)
        {
            var modelId = NewId.NextGuid();
            var blobInfo = blob.BlobInfo;
            var metadata = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            if (blobInfo.Metadata != null)
                blobInfo.Metadata.ToList().ForEach(kv => metadata[kv.Key] = kv.Value);

            Guid userId = blobInfo.UserId.HasValue ? blobInfo.UserId.Value : new Guid(metadata[nameof(userId)].ToString());
            Guid parentId = metadata.ContainsKey(nameof(parentId)) ? new Guid(metadata[nameof(parentId)].ToString()) : userId;

            var metaJson = metadata["ModelInfo"].ToString();
            var meta = JsonConvert.DeserializeObject<Dictionary<string, object>>(metaJson);

            var method = meta.ContainsKey("Method") ? meta["Method"].ToString() : "";
            var className = meta.ContainsKey("ClassName") ? meta["ClassName"].ToString() : "";
            var modelName = meta.ContainsKey("ModelName") ? meta["ModelName"].ToString() : "";
            var subSambleSize = meta.ContainsKey("SubSampleSize") ? Convert.ToDouble(meta["SubSampleSize"].ToString()) : 0.0;
            var testDataSetSize = meta.ContainsKey("TestDatasetSize") ? Convert.ToDouble(meta["TestDatasetSize"].ToString()) : 0.0;
            var kFold = meta.ContainsKey("KFold") ? Convert.ToInt32(meta["KFold"].ToString()) : 0;
            var fingerPrints = meta.ContainsKey("Fingerprints") ? JsonConvert.DeserializeObject<List<Fingerprint>>(meta["Fingerprints"].ToString()) : new List<Fingerprint>();
            var scaler = meta.ContainsKey("Scaler") ? meta["Scaler"].ToString() : "";
            var dataset = meta.ContainsKey("Dataset") ? JsonConvert.DeserializeObject<Dataset>(meta["Dataset"].ToString()) : new Dataset("no title", "no desctiption");
            var property = meta.ContainsKey("Property") ? JsonConvert.DeserializeObject<Property>(meta["Property"].ToString()) : new Property("no category", "no name", "no units", "no description");
            var displayMethodName = meta.ContainsKey("MethodDisplayName") ? meta["MethodDisplayName"].ToString() : method;

            var model = new Model(modelId, userId, parentId, ModelStatus.Loaded, method, scaler, kFold, (decimal)testDataSetSize, (decimal)subSambleSize,
                className, fingerPrints, dataset, property, blobInfo.Id, blobInfo.Bucket, modelName, displayMethodName, meta);

            await _session.Add(model);
            await _session.Commit();
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static void UseInMemoryModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, MachineLearningModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.AddImageCommandHandler>();
            services.AddScoped<BackEnd.ChangeStatusCommandHandler>();
            services.AddScoped<BackEnd.CreateModelCommandHandler>();
            services.AddScoped<BackEnd.DeleteModelCommandHandler>();
            services.AddScoped<BackEnd.MoveModelCommandHandler>();
            services.AddScoped<BackEnd.UpdateModelPropertiesCommandHandler>();
            services.AddScoped<BackEnd.UpdateModelNameCommandHandler>();
            services.AddScoped<BackEnd.GrantAccessCommandHandler>();
            services.AddScoped<BackEnd.SetTargetsCommandHandler>();
            services.AddScoped<BackEnd.SetConsensusWeightCommandHandler>();
            services.AddScoped<BackEnd.UpdateModelBlobCommandHandler>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.ModelsEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();

            //  add state machines...
            services.AddSingleton<ModelTrainingStateMachine>();
            services.AddSingleton<PropertiesPredictionStateMachine>();
            services.AddSingleton<TrainingStateMachine>();

            //  add state machines repositories...
            services.AddSingleton<ISagaRepository<ModelTrainingState>>(new InMemorySagaRepository<ModelTrainingState>());
            services.AddSingleton<ISagaRepository<PropertiesPredictionState>>(new InMemorySagaRepository<PropertiesPredictionState>());
            services.AddSingleton<ISagaRepository<TrainingState>>(new InMemorySagaRepository<TrainingState>());
        }

        public static void UseBackEndModule(this IServiceCollection services)
        {
            services.AddScoped<IModule, MachineLearningModule>();

            //  add backend consumers...
            services.AddScoped<BackEnd.AddImageCommandHandler>();
            services.AddScoped<BackEnd.ChangeStatusCommandHandler>();
            services.AddScoped<BackEnd.CreateModelCommandHandler>();
            services.AddScoped<BackEnd.DeleteModelCommandHandler>();
            services.AddScoped<BackEnd.MoveModelCommandHandler>();
            services.AddScoped<BackEnd.UpdateModelPropertiesCommandHandler>();
            services.AddScoped<BackEnd.UpdateModelNameCommandHandler>();
            services.AddScoped<BackEnd.GrantAccessCommandHandler>();
            services.AddScoped<BackEnd.SetTargetsCommandHandler>();
            services.AddScoped<BackEnd.SetConsensusWeightCommandHandler>();
            services.AddScoped<BackEnd.UpdateModelBlobCommandHandler>();
        }

        public static void UsePersistenceModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, MachineLearningModule>();

            //  add persistence consumers...
            services.AddTransient<Persistence.EventHandlers.ModelsEventHandlers>();
            services.AddTransient<Persistence.EventHandlers.NodesEventHandlers>();
        }

        public static void UseSagaHostModule(this IServiceCollection services)
        {
            services.AddTransient<IModule, MachineLearningModule>();

            //  add state machines...
            services.AddSingleton<ModelTrainingStateMachine>();
            services.AddSingleton<PropertiesPredictionStateMachine>();
            services.AddSingleton<TrainingStateMachine>();
        }
    }

    public static class ConfigurationExtensions
    {
        public static void RegisterInMemoryModule(this IBusFactoryConfigurator configurator, IServiceProvider provider)
        {
            //  register backend consumers...
            configurator.RegisterScopedConsumer<BackEnd.AddImageCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.ChangeStatusCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CreateModelCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.DeleteModelCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.MoveModelCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.UpdateModelBlobCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.UpdateModelNameCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.GrantAccessCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.SetTargetsCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.SetConsensusWeightCommandHandler>(provider, null, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.UpdateModelPropertiesCommandHandler>(provider, null, c => c.UseCqrsLite());

            //  register persistence consumers...
            configurator.RegisterConsumer<Persistence.EventHandlers.ModelsEventHandlers>(provider);
            configurator.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(provider);

            //  register state machines...
            configurator.RegisterStateMachine<ModelTrainingStateMachine, ModelTrainingState>(provider);
            configurator.RegisterStateMachine<PropertiesPredictionStateMachine, PropertiesPredictionState>(provider);
            configurator.RegisterStateMachine<TrainingStateMachine, TrainingState>(provider);
        }

        public static void RegisterBackEndModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register backend consumers...
            configurator.RegisterScopedConsumer<BackEnd.AddImageCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.ChangeStatusCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.CreateModelCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.DeleteModelCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.MoveModelCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.UpdateModelPropertiesCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.UpdateModelNameCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.GrantAccessCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.SetTargetsCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.SetConsensusWeightCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
            configurator.RegisterScopedConsumer<BackEnd.UpdateModelBlobCommandHandler>(host, provider, endpointConfigurator, c => c.UseCqrsLite());
        }

        public static void RegisterPersistenceModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            //  register persistence consumers...
            configurator.RegisterConsumer<Persistence.EventHandlers.ModelsEventHandlers>(host, provider, endpointConfigurator);
            configurator.RegisterConsumer<Persistence.EventHandlers.NodesEventHandlers>(host, provider, endpointConfigurator);
        }

        public static void RegisterSagaHostModule(this IRabbitMqBusFactoryConfigurator configurator, IRabbitMqHost host, IServiceProvider provider, Action<IRabbitMqReceiveEndpointConfigurator> endpointConfigurator = null)
        {
            var repositoryFactory = provider.GetRequiredService<ISagaRepositoryFactory>();

            //  register state machines...
            configurator.RegisterStateMachine<ModelTrainingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
            configurator.RegisterStateMachine<PropertiesPredictionStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
            configurator.RegisterStateMachine<TrainingStateMachine>(host, provider, repositoryFactory, endpointConfigurator);
        }
    }
}
