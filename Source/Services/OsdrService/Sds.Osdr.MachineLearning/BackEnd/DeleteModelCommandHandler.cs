using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain.Commands.Models;
using Sds.Osdr.MachineLearning.Domain;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class DeleteModelCommandHandler : IConsumer<DeleteModel>
    {
        private readonly ISession session;

        public DeleteModelCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<DeleteModel> context)
        {
            var model = await session.Get<Model>(context.Message.Id);

            model.DeleteModel(context.Message.UserId, context.Message.Force);

            await session.Commit();
        }
    }
}
