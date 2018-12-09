using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Records
{
    public class CreateInvalidRecordEventHandlers : IConsumer<CreateInvalidRecord>
    {
        private readonly ISession _session;

        public CreateInvalidRecordEventHandlers(ISession session)
        {
            this._session = session ?? throw new ArgumentNullException(nameof(session));
        }
        public async Task Consume(ConsumeContext<CreateInvalidRecord> context)
        {
            var invalidRecord = new InvalidRecord(context.Message.Id, context.Message.UserId, RecordType.Structure, context.Message.FileId, context.Message.Index, context.Message.Message);

            await _session.Add(invalidRecord);

            await _session.Commit();
        }
    }
}
