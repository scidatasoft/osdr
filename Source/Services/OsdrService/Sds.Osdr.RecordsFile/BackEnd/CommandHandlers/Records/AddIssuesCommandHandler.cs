using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.RecordsFile.BackEnd.CommandHandlers.Records
{
    public class AddIssuesCommandHandler : IConsumer<AddIssues>
    {
        private readonly ISession session;

        public AddIssuesCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<AddIssues> context)
        {
            var record = await session.Get<Record>(context.Message.Id);

            record.AddIssues(context.Message.UserId, context.Message.Issues);

            await session.Commit();
        }
    }
}
