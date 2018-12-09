using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class SetTargetsCommandHandler: IConsumer<SetTargets>
    {
        private readonly ISession session;

        public SetTargetsCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<SetTargets> context)
        {
            var model = await session.Get<Model>(context.Message.Id);

            model.SetTargets(context.Message.Targets, context.Message.UserId);

            await session.Commit();
        }
    }
}
