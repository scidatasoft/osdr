using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Crystals.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Crystals.BackEnd.CommandHandlers
{
    public class CreateCrystalCommandHandler : IConsumer<CreateCrystal>
    {
        private readonly ISession session;

        public CreateCrystalCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<CreateCrystal> context)
        {
            var reaction = new Domain.Crystal(context.Message.Id, context.Message.Bucket, context.Message.BlobId, context.Message.UserId, context.Message.FileId, context.Message.Index, context.Message.Fields);

            await session.Add(reaction);

            await session.Commit();
        }
    }
}
