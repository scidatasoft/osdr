using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Sds.Osdr.Generic.Domain.Events.Users;
using Sds.Osdr.WebApi.Hubs;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.EventHandlers
{
    public class UserPersistedEventHandler : IConsumer<UserPersisted> 
    {
        IHubContext<OrganizeHub> _hubContext;

        public UserPersistedEventHandler (IHubContext<OrganizeHub> hubContext)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        public Task Consume(ConsumeContext<UserPersisted> context)
        {
            _hubContext.Clients.User(context.Message.Id.ToString()).SendAsync("userProfileUpdated", new { id = context.Message.Id, version = context.Message.Version });

            return Task.CompletedTask;
        }
    }
}
