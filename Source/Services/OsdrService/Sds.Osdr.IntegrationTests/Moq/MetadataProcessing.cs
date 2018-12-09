using MassTransit;
using Sds.MetadataStorage.Domain.Commands;
using Sds.MetadataStorage.Domain.Events;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class MetadataProcessing : IConsumer<GenerateMetadata>
    {
        public async Task Consume(ConsumeContext<GenerateMetadata> context)
        {
            await context.Publish<MetadataGenerated>(new
            {
                Id = context.Message.FileId
            });
        }
    }
}
