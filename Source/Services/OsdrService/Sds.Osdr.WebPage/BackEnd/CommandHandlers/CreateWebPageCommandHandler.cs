using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.WebPage.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.WebPage.BackEnd.CommandHandlers
{
    public class CreateWebPageCommandHandler : IConsumer<CreateWebPage>
    {
        private readonly ISession session;

        public CreateWebPageCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<CreateWebPage> context)
        {
            var message = context.Message;

            var page = new Domain.WebPage(message.Id, message.UserId, message.ParentId, message.Name, message.Status, message.Bucket, message.BlobId, message.Lenght, message.Md5, message.Url);

            await session.Add(page);

            await session.Commit();
        }
    }
}
