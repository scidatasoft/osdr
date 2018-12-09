using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Office.Domain;
using Sds.Osdr.Office.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Office.BackEnd.CommandHandlers
{
    public class AddMetadataCommandHandler : IConsumer<AddMetadata>
    {
        private readonly ISession session;

        public AddMetadataCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<AddMetadata> context)
        {
            var file = await session.Get<OfficeFile>(context.Message.Id);

            file.AddMetadata(context.Message.UserId, context.Message.Metadata);

            await session.Commit();
        }
    }
}
