using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Files;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.BackEnd.CommandHandlers.Files
{
    public class AddImageCommandHandler : IConsumer<AddImage>
    {
        private readonly ISession session;

        public AddImageCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<AddImage> context)
        {
            var file = await session.Get<File>(context.Message.Id);

            file.AddImage(context.Message.UserId, context.Message.Image);

            await session.Commit();
        }
    }
}
