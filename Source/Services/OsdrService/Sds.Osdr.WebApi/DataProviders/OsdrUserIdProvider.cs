using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Sds.Osdr.WebApi.DataProviders
{
    public class OsdrUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
