using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class SetConsensusWeightCommandHandler : IConsumer<SetConsensusWeight>
    {
        private readonly ISession session;

        public SetConsensusWeightCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<SetConsensusWeight> context)
        {
            var model = await session.Get<Model>(context.Message.Id);

            model.SetConsensusWeight(context.Message.ConsensusWeight, context.Message.UserId);

            await session.Commit();
        }
    }
}
