using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain.Commands.Files;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Files
{
    public class UpdateTotalRecordsCommandHandler : IConsumer<UpdateTotalRecords>
    {
        private readonly ISession session;

        public UpdateTotalRecordsCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<UpdateTotalRecords> context)
        {
            var file = await session.Get<Domain.RecordsFile>(context.Message.Id);

            file.UpdateTotalRecords(context.Message.UserId, context.Message.TotalRecords);

            await session.Commit();
        }
    }
}
