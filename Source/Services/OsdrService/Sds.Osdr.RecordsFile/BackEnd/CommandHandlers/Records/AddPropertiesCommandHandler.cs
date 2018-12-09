using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Records
{
    public class AddPropertiesCommandHandler : IConsumer<AddProperties>
    {
        private readonly ISession session;

        public AddPropertiesCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<AddProperties> context)
        {
            var record = await session.Get<Record>(context.Message.Id);

            record.AddProperties(context.Message.UserId, context.Message.Properties);

            await session.Commit();
        }
    }
}
