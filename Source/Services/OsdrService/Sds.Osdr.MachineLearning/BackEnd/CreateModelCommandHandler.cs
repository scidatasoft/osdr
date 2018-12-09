using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class CreateModelCommandHandler : IConsumer<CreateModel>
    {
        private readonly ISession session;

        public CreateModelCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<CreateModel> context)
        {
            var e = context.Message;

            var model = new Model(e.Id, e.UserId, e.ParentId, ModelStatus.Created, e.Method, e.Scaler, e.KFold, e.TestDatasetSize, e.SubSampleSize, e.ClassName, e.Fingerprints);

            await session.Add(model);

            await session.Commit();
        }
    }
}
