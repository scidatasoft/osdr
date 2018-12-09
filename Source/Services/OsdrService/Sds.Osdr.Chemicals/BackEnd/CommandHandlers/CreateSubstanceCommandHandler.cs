using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Chemicals.Domain.Aggregates;
using Sds.Osdr.Chemicals.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Chemicals.BackEnd.CommandHandlers
{
    public class CreateSubstanceCommandHandler : IConsumer<CreateSubstance>
    {
        private readonly ISession _session;

        public CreateSubstanceCommandHandler(ISession session)
        {
            this._session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<CreateSubstance> context)
        {
            var substance = new Substance(context.Message.Id, context.Message.Bucket, context.Message.BlobId, context.Message.UserId, context.Message.FileId, context.Message.Index, context.Message.Fields);

            await _session.Add(substance);

            await _session.Commit();
        }
    }
}
