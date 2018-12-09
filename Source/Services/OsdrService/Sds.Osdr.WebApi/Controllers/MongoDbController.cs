using System;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using Sds.Osdr.WebApi.Filters;
using System.Linq;

namespace Sds.Osdr.WebApi.Controllers
{
    [Authorize]
    [UserInfoRequired]
    public abstract class MongoDbController : Controller
    {
        protected IMongoDatabase Database;
        protected Guid? UserId => User.Claims.Where(c => c.Type.Contains("/nameidentifier")).FirstOrDefault() != null ? (Guid?)Guid.Parse(User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value) : null;
        
        public MongoDbController(IMongoDatabase database)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
        }
    }
}