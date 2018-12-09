using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.WebPage.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.WebPage.BackEnd.CommandHandlers
{
    public class UpdateWebPageCommandHandler : IConsumer<UpdateWebPage>
    {
        private readonly ISession session;

        public UpdateWebPageCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<UpdateWebPage> context)
        {
            var message = context.Message;

            var file = await session.Get<Domain.WebPage>(message.Id);

            file.UpdateWebPage(message.UserId, message.Bucket, message.JsonBlobId);

            await session.Commit();
        }
    }
}
