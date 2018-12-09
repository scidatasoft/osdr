using MassTransit;
using Sds.ChemicalProperties.Domain.Commands;
using Sds.ChemicalProperties.Domain.Events;
using Sds.ChemicalProperties.Domain.Models;
using Sds.Cvsp.Compounds;
using Sds.Storage.Blob.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sds.ChemicalProperties.Processing.CommandHandlers
{
    public class ChemicalPropertiesCommandHandler : IConsumer<CalculateChemicalProperties>
    {
        PropertiesCalculation _propertiesCalculation;
        IBlobStorage _blobStorage;

        public ChemicalPropertiesCommandHandler(PropertiesCalculation propertiesCalculation, IBlobStorage blobStorage)
        {
            _propertiesCalculation = propertiesCalculation ?? throw new ArgumentNullException(nameof(propertiesCalculation));
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }

        public async Task Consume(ConsumeContext<CalculateChemicalProperties> context)
        {
            using (var blob = await _blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket))
            {
                StreamReader reader = new StreamReader(blob.GetContentAsStream());
                string mol = reader.ReadToEnd();

                try
                {
                    var cvspResult = _propertiesCalculation.Calculate(mol);
                    var result = new CalculatedProperties
                    {
                        Issues = cvspResult.Issues,
                        Properties = cvspResult.Properties
                    };

                    await context.Publish<ChemicalPropertiesCalculated>(new
                    {
                        Id = context.Message.Id,
                        Result = result,
                        CorrelationId = context.Message.CorrelationId,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    await context.Publish<ChemicalPropertiesCalculationFailed>(new
                    {
                        Id = context.Message.Id,
                        CalculationException = ex,
                        CorrelationId = context.Message.CorrelationId,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow
                    });
                }
            }
        }
    }
}
