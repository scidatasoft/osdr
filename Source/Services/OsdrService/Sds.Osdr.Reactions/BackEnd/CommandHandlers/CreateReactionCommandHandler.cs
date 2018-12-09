using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Reactions.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Reactions.BackEnd.CommandHandlers
{
    public class CreateReactionCommandHandler : IConsumer<CreateReaction>
    {
        private readonly ISession session;

        public CreateReactionCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<CreateReaction> context)
        {
            var reaction = new Domain.Reaction(context.Message.Id, context.Message.Bucket, context.Message.BlobId, context.Message.UserId, context.Message.FileId, context.Message.Index, context.Message.Fields);

            await session.Add(reaction);

            await session.Commit();
        }
    }
}
