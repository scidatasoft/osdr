using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Records
{
    public class SetAccessPermissionsCommandHandler : IConsumer<SetAccessPermissions>
    {
        private readonly ISession session;

        public SetAccessPermissionsCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<SetAccessPermissions> context)
        {
            var record = await session.Get<Record>(context.Message.Id);

            record.SetAccessPermissions(context.Message.UserId, context.Message.Permissions);

            await session.Commit();
        }
    }
}
