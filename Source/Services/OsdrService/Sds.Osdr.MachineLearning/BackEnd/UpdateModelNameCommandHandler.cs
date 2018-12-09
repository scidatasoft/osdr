using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class UpdateModelNameCommandHandler : IConsumer<UpdateModelName>
    {
        private readonly ISession session;

        public UpdateModelNameCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<UpdateModelName> context)
        {
            var model = await session.Get<Model>(context.Message.Id);

            model.UpdateModelName(context.Message.UserId, context.Message.ModelName);
            
            await session.Commit();
        }
    }
}
