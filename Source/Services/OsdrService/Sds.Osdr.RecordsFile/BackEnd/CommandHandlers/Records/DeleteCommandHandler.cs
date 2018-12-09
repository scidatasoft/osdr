using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Records
{
    public class DeleteCommandHandler : IConsumer<DeleteRecord>
    {
        private readonly ISession _session;

        public DeleteCommandHandler(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<DeleteRecord> context)
        {
            var record = await _session.Get<Record>(context.Message.Id);
                
            record.Delete(context.Message.Id, context.Message.UserId, context.Message.Force);

            await _session.Commit();
        }
    }
}
