using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class UpdateModelPropertiesCommandHandler : IConsumer<UpdateModelProperties>
    {
        private readonly ISession session;

        public UpdateModelPropertiesCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<UpdateModelProperties> context)
        {
            var model = await session.Get<Model>(context.Message.Id);

            model.UpdateModelProperties(context.Message.UserId, context.Message.Dataset, context.Message.Property, context.Message.Modi, context.Message.DisplayMethodName);

            await session.Commit();
        }
    }
}
