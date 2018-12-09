using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class MoveModelCommandHandler : IConsumer<MoveModel>
    {
        private readonly ISession session;

        public MoveModelCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<MoveModel> context)
        {
            var model = await session.Get<Model>(context.Message.Id);

            model.MoveModel(context.Message.UserId, context.Message.NewParentId);

            await session.Commit();
        }
    }
}
