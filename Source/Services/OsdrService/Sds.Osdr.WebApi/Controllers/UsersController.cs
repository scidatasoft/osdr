using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Commands.Users;
using Sds.Osdr.WebApi.Extensions;
using Sds.Osdr.WebApi.Requests;
using Sds.Osdr.WebApi.Responses;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : MongoDbController
    {
        private IBusControl _bus;
        private IMongoCollection<dynamic> _users;
        private IUrlHelper _urlHelper;

        public UsersController(IMongoDatabase database, IBusControl bus, IUrlHelper urlHelper)
            : base(database)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _users = database.GetCollection<dynamic>("Users");
            _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
        }

        /// <summary>
        /// Creating new user profile
        /// </summary>
        /// <param name="request">Info abot new user profile (display name, first name, last name etc.)</param>
        /// <param name="id">User id</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> CreateOrUpdate(Guid id, [FromBody]CreateOrUpdateUserRequest request)
        {
            if (request is null)
                return BadRequest();

            if (!await _users.Find(new BsonDocument("_id", id)).AnyAsync())
            {
                await _bus.Publish<CreateUser>(new
                {
                    Id = id,
                    request.DisplayName,
                    request.FirstName,
                    request.LastName,
                    request.LoginName,
                    request.Email,
                    request.Avatar,
                    UserId
                });
            }
            else
            {
                await _bus.Publish<UpdateUser>(new
                {
                    Id = id,
                    NewDisplayName = request.DisplayName,
                    NewFirstName = request.FirstName,
                    NewLastName = request.LastName,
                    request.LoginName,
                    NewEmail = request.Email,
                    NewAvatar = request.Avatar,
                    UserId
                });
            }

            return AcceptedAtRoute("GetUserById", new { id }, null);
        }

        /// <summary>
        /// Get user by id
        /// </summary>
        /// <param name="id">User Id</param>
        /// <returns></returns>
        [HttpGet("{id:guid}", Name = "GetUserById")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var filter = new OrganizeFilter().ById(id);

            var user = await _users.Find(new BsonDocument("_id", id)).FirstOrDefaultAsync();

            if (user is null)
            {
                Log.Error($"User {id} not found");
                return NotFound();
            }
            else if (((IDictionary<String, Object>)user)["_id"].ToString() != UserId.ToString())
            {
                return Forbid();
            }
            ((IDictionary<string, object>)user).Remove("_id");
            ((IDictionary<string, object>)user)["id"] = id;

            return Ok(user);
        }

        /// <summary>
        /// Get current user info
        /// </summary>
        /// <returns></returns>
        [HttpGet("~/api/me", Name = "GetCurrentUser")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _users.Find(new BsonDocument("_id", UserId)).FirstOrDefaultAsync();

            if (user is null)
            {
                Log.Error($"User {UserId} not found");
                return NotFound();
            }
            ((IDictionary<string, object>)user).Remove("_id");
            ((IDictionary<string, object>)user)["id"] = UserId;

            return Ok(user);
        }


        /// <summary>
        /// Get user public information by id
        /// </summary>
        /// <param name="id">User Id</param>
        /// <returns></returns>
        [AllowAnonymous]
        [ResponseCache(Duration = 260)]
        [HttpGet("{id:guid}/public-info", Name = "GetUserPublicInfo")]
        public async Task<IActionResult> GetDisplayInfo(Guid id)
        {
            var user = await Database.GetCollection<UserPublicInfo>("Users").Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user is null)
                return NotFound();

            return Ok(user);
        }
    }
}