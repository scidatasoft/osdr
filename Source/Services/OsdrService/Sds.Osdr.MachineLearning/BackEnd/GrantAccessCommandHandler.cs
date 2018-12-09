using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sds.Osdr.MachineLearning.BackEnd
{
    public class GrantAccessCommandHandler : IConsumer<GrantAccess>
    {
        private readonly ISession session;

        public GrantAccessCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<GrantAccess> context)
        {
            var file = await session.Get<Model>(context.Message.Id);

            file.GrantAccess(context.Message.UserId, context.Message.Permissions);

            await session.Commit();
        }
    }
}
