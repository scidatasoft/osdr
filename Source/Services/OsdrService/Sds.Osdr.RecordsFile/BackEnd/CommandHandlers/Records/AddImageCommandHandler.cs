using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Records
{
    public class AddImageCommandHandler : IConsumer<AddImage>
    {
        private readonly ISession _session;

        public AddImageCommandHandler(ISession session)
        {
            this._session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<AddImage> context)
        {
            var record = await _session.Get<Record>(context.Message.Id);

            record.AddImage(context.Message.UserId, context.Message.Image);

            await _session.Commit();
        }
    }
}
