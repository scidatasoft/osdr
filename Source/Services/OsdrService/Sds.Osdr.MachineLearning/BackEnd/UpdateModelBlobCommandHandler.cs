using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class UpdateModelBlobCommandHandler : IConsumer<UpdateModelBlob>
    {
        private readonly ISession session;

        public UpdateModelBlobCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<UpdateModelBlob> context)
        {
            var model = await session.Get<Model>(context.Message.Id);

            model.SetMetadata(context.Message.UserId, context.Message.BlobId, context.Message.Bucket, context.Message.Metadata);
            
            await session.Commit();
        }
    }
}
