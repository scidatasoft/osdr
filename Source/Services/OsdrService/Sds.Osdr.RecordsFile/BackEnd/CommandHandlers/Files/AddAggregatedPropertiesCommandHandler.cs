using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain.Commands.Files;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Files
{
    public class AddAggregatedPropertiesCommandHandler : IConsumer<AddAggregatedProperties>
    {
        private readonly ISession session;

        public AddAggregatedPropertiesCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<AddAggregatedProperties> context)
        {
            var file = await session.Get<Domain.RecordsFile>(context.Message.Id);

            file.AddChemicalProperties(context.Message.UserId, context.Message.Properties);

            await session.Commit();
        }
    }
}
