using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class AddImageCommandHandler : IConsumer<AddImage>
    {
        private readonly ISession session;

        public AddImageCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<AddImage> context)
        {
            var model = await session.Get<Model>(context.Message.Id);

            model.AddImage(context.Message.UserId, context.Message.Image);

            //await session.Add(model);

            await session.Commit();
        }
    }
}
