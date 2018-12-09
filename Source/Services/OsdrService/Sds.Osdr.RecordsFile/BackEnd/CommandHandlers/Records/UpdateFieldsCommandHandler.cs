using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain.Events;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Records
{
    public class UpdateFieldsCommandHandler : IConsumer<UpdateFields>
    {
        private readonly ISession session;

        public UpdateFieldsCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }
                
        public async Task Consume(ConsumeContext<UpdateFields> context)
        {
            var record = await session.Get<Record>(context.Message.Id);
            if (record.Version == context.Message.ExpectedVersion)
            {
                record.UpdateFields(context.Message.UserId, context.Message.Fields);
                await session.Commit();
            }
            else
            {
                Log.Error($"Unexpected version for record '{context.Message.Id}', expected version {context.Message.ExpectedVersion}, found {record.Version}");
                await context.Publish<UnexpectedVersion>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    Version = context.Message.ExpectedVersion,
                    TimeStamp = DateTimeOffset.Now
                });
            }
        }
    }
}
