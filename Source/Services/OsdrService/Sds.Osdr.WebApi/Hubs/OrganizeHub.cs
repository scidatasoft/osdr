using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Sds.Osdr.WebApi.ConnectedUsers;
using System;
using Serilog;
using System.Linq;

namespace Sds.Osdr.WebApi.Hubs
{
    public class OrganizeHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            var identity = Context.User.Identity as System.Security.Claims.ClaimsIdentity;
            if (identity != null)
            {
                var userId = identity.Claims.Where(c => c.Type.Contains("/nameidentifier")).First();
                if (userId != null)
                {
                    return;
                }
            }

            Log.Warning("OrganizeHub: Can't get user ID. Messages will not be sent.");
        }
    }
}
