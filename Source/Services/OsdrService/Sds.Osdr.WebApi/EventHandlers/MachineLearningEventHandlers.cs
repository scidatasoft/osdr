using MassTransit;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Osdr.WebApi.Extensions;
using Sds.Reflection;
using Sds.Storage.KeyValue.Core;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.EventHandlers
{
    public class MachineLearningEventHandlers : IConsumer<PredictedResultReady>, IConsumer<ReportReady>, 
        //IConsumer<CalculateFeatureVectors>,
        IConsumer<FeatureVectorsCalculated>,
        IConsumer<FeatureVectorsCalculationFailed>
    {
        private IKeyValueRepository _keyValueRepository;
        private SingleStructurePredictionSettings _sspSettings;
        private IBusControl _bus;

        public MachineLearningEventHandlers(IKeyValueRepository keyValueRepository, IBusControl bus, IOptions<SingleStructurePredictionSettings> sspSettings)
        {
            _keyValueRepository = keyValueRepository ?? throw new ArgumentNullException(nameof(keyValueRepository));
            _sspSettings = sspSettings?.Value ?? throw new ArgumentNullException(nameof(sspSettings));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public Task Consume(ConsumeContext<PredictedResultReady> context)
        {
            var prediction = _keyValueRepository.LoadObject<dynamic>(context.Message.Id);
            var softwareInfo = $@"{{
                              'software': '{_sspSettings.Software}',
                              'version': '{Assembly.GetEntryAssembly().GetVersion()}'
                        }}";
            var softwareInfoObj = JsonConvert.DeserializeObject<dynamic>(softwareInfo);
            var response = context.Message.Data;
            response.provider = softwareInfoObj;
            prediction.response = response;

            prediction.status = "COMPLETE";
            _keyValueRepository.SaveObject(context.Message.Id, prediction);
            _keyValueRepository.SetExpiration(context.Message.Id, TimeSpan.Parse(_sspSettings.RedisExpirationTime));
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<ReportReady> context)
        {
            throw new NotImplementedException();
        }

        public Task Consume(ConsumeContext<FeatureVectorsCalculated> context)
        {
            var resultName = $"{context.Message.CorrelationId}-result";
            SaveMessage(resultName, context.Message);
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<FeatureVectorsCalculationFailed> context)
        {
            var resultName = $"{context.Message.CorrelationId}-error";
            SaveMessage(resultName, context.Message);
            return Task.CompletedTask;
        }

        //public async Task Consume(ConsumeContext<CalculateFeatureVectors> context)
        //{
        //    var resultName = $"{context.Message.CorrelationId}-csv";
        //    var fileBytes = File.ReadAllBytes("FocusSynthesis_InStock_071411.csv");
        //    _keyValueRepository.SaveData(resultName, fileBytes);
        //    _keyValueRepository.SetExpiration(resultName, TimeSpan.Parse(_sspSettings.RedisExpirationTime));

        //    await _bus.Publish(new FeatureVectorsCalculated { Structures = 15, Columns = 18, Failed = 0, CorrelationId = context.Message.CorrelationId });
        //}

        private void SaveMessage(string key, object message)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, message);
            _keyValueRepository.SaveData(key, ms.ToArray());
            _keyValueRepository.SetExpiration(key, TimeSpan.Parse(_sspSettings.RedisExpirationTime));
        }





    }
}
