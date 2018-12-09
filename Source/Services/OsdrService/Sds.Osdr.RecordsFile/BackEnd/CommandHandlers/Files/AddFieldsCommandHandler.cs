using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain.Commands.Files;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Files
{
    public class AddFieldsCommandHandler : IConsumer<AddFields>
    {
        private readonly ISession session;

        public AddFieldsCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<AddFields> context)
        {
            var record = await session.Get<Domain.RecordsFile>(context.Message.Id);

            record.AddFields(context.Message.UserId, context.Message.Fields);

            await session.Commit();
        }
    }
}
