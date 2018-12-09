using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.WebPage.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.WebPage.BackEnd.CommandHandlers
{
    public class UpdateTotalRecordsCommandHandler : IConsumer<UpdateTotalWebRecords>
    {
        private readonly ISession session;

        public UpdateTotalRecordsCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }


        public async Task Consume(ConsumeContext<UpdateTotalWebRecords> context)
        {
            var message = context.Message;

            var file = await session.Get<Domain.WebPage>(message.Id);

            file.UpdateTotalRecords(message.UserId, message.TotalRecords);

            await session.Commit();

        }
    }
}
