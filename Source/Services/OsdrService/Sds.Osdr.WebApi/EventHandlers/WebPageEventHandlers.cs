using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Sds.Osdr.Generic.Domain.Events;
using Sds.Osdr.WebApi.ConnectedUsers;
using Sds.Osdr.WebApi.Hubs;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.EventHandlers
{
    public class WebPageEventHandlers : IConsumer<OperationFailed>,
                                        IConsumer<ProcessingStarted>,
                                        IConsumer<ProcessingFailed>,
                                        IConsumer<ProcessingFinished>
    {
        IHubContext<OrganizeHub> _hubContext;

        public WebPageEventHandlers(IConnectedUserManager connectedUserManager, IHubContext<OrganizeHub> hubContext)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        public async Task Consume(ConsumeContext<OperationFailed> context)
        {
            var message = context.Message;
            await _hubContext.Clients.User(message.UserId.ToString())
                .SendAsync("generalEvents", new
                {
                    Id = message.Id,
                    EventName = message.GetType().Name,
                    NodeType = message.NodeType,
                    EventData = message.Message
                });
        }

        public async Task Consume(ConsumeContext<ProcessingStarted> context)
        {
            var message = context.Message;
            await _hubContext.Clients.User(message.UserId.ToString())
                .SendAsync("generalEvents", new
                {
                    Id = message.Id,
                    EventName = message.GetType().Name,
                    NodeType = message.NodeType,
                    EventData = message.Message
                });
        }


        public async Task Consume(ConsumeContext<ProcessingFailed> context)
        {
            var message = context.Message;
            await _hubContext.Clients.User(message.UserId.ToString())
                .SendAsync("generalEvents", new
                {
                    Id = message.Id,
                    EventName = message.GetType().Name,
                    NodeType = message.NodeType,
                    EventData = message.Message
                });
        }


        public async Task Consume(ConsumeContext<ProcessingFinished> context)
        {
            var message = context.Message;
            await _hubContext.Clients.User(message.UserId.ToString())
                .SendAsync("generalEvents", new
                {
                    Id = message.Id,
                    EventName = message.GetType().Name,
                    NodeType = message.NodeType,
                    EventData = message.Message
                });
        }
    }
}
