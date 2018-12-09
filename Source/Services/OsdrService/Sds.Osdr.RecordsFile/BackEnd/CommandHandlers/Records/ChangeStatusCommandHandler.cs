using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Records
{
    public class ChangeStatusCommandHandler : IConsumer<ChangeStatus>
    {
        private readonly ISession session;

        public ChangeStatusCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<ChangeStatus> context)
        {
            var record = await session.Get<Record>(context.Message.Id);

            record.ChangeStatus(context.Message.UserId, context.Message.Status);

            await session.Commit();
        }
    }
}
