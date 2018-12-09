using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Files;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.BackEnd.CommandHandlers.Files
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
            var file = await session.Get<File>(context.Message.Id);

            file.ChangeStatus(context.Message.UserId, context.Message.Status);

            await session.Commit();
        }
    }
}
