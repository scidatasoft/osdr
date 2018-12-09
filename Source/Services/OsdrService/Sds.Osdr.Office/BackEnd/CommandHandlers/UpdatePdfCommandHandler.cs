using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Office.Domain;
using Sds.Osdr.Office.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Office.BackEnd.CommandHandlers
{
    public class UpdatePdfCommandHandler : IConsumer<UpdatePdf>
    {
        private readonly ISession session;

        public UpdatePdfCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<UpdatePdf> context)
        {
            var file = await session.Get<OfficeFile>(context.Message.Id);

            file.UpdatePdf(context.Message.UserId, context.Message.Bucket, context.Message.BlobId);

            await session.Commit();
        }
    }
}
