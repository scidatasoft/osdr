using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Files;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.BackEnd.CommandHandlers.Files
{
    public class GrantAccessCommandHandler : IConsumer<GrantAccess>
    {
        private readonly ISession session;

        public GrantAccessCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<GrantAccess> context)
        {
            var file = await session.Get<File>(context.Message.Id);

            file.GrantAccess(context.Message.UserId, context.Message.Permissions);

            await session.Commit();
        }
    }
}
