using System;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MassTransit;
using Sds.Osdr.WebApi.Extensions;
using MongoDB.Bson;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Sds.Osdr.WebApi.Requests;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using Sds.Osdr.Generic.Domain.Commands.UserNotifications;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class UserNotificationsController : MongoDbController, IPaginationController
    {
        IBusControl _bus;
        IUrlHelper _urlHelper;

        public UserNotificationsController(IMongoDatabase database, IBusControl bus, IUrlHelper urlHelper)
            : base(database)
        {
                _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
        }

        [NonAction]
        public string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, Guid? containerId = null, string filter = null, IEnumerable<string> fields = null)
        {
            int pageNumber = uriType == PaginationUriType.PreviousPage ? request.PageNumber - 1 : request.PageNumber + 1;
            return _urlHelper.Link(action, new RouteValueDictionary
                {
                    { "pageSize",  request.PageSize },
                    { "pageNumber", pageNumber }
                });
        }

        [HttpGet(Name = "GetUserNotifications")]
        public async Task<IActionResult> GetList([FromQuery]PaginationRequest request, [FromQuery]bool unreadOnly = true)
        {
            BsonDocument filter = new OrganizeFilter(UserId.Value);
            if (unreadOnly)
                filter.Add("IsRead", new BsonDocument("$ne", true));
            
            var notifications = await Database.GetCollection<dynamic>("UserNotifications").Find(filter).Sort("{TimeStamp:-1}").ToPagedListAsync(request.PageNumber, request.PageSize);
            this.AddPaginationHeader(request, notifications, "GetUserNotifications");

            return Ok(notifications);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            BsonDocument filter = new OrganizeFilter(UserId.Value);

            var notifications = await Database.GetCollection<dynamic>("UserNotifications").Find(filter).ToListAsync();

            notifications.AsParallel()
                .ForAll(n => _bus.Publish<DeleteUserNotification>(new { Id = n._id }).Wait());

            return Accepted();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _bus.Publish<DeleteUserNotification>(new { Id = id });

            return Accepted();
        }

        [NonAction]
        public string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, RouteValueDictionary routeValueDictionary)
        {
            int pageNumber = uriType == PaginationUriType.PreviousPage ? request.PageNumber - 1 : request.PageNumber + 1;

            return _urlHelper.Link(action, routeValueDictionary);
        }
    }
}