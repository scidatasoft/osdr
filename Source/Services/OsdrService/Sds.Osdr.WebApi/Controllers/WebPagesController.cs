using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sds.Osdr.WebApi.Filters;
using Sds.Osdr.WebApi.Requests;
using Sds.Osdr.WebPage.Sagas.Commands;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [UserInfoRequired]
    public class WebPagesController : Controller
    {
        private IBusControl _bus;
        protected Guid UserId => Guid.Parse(User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value);

        public WebPagesController(IBusControl bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));

        /// <summary>
        /// Import web page from specified uri
        /// </summary>
        /// <remarks>
        /// Note that parentId and bucket is optional, current user id is default value for them. 
        ///  
        ///     POST api/webpages
        ///     {
        ///        "uri": "http://www.w3.org"
        ///     }
        /// 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <response code="202">if start processing</response>
        /// <response code="400">if uri is null</response>
        [ProducesResponseType(202)]
        [ProducesResponseType(400)]
        [HttpPost]
        public async Task<IActionResult> Import([FromBody]ImportWebPageRequest request)
        {
            if (string.IsNullOrEmpty(request.Bucket))
                request.Bucket = User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value;
            if (!request.Uri.StartsWith("http://") && !request.Uri.StartsWith("https://"))
                request.Uri = "https://" + request.Uri;
            Log.Information($"User {UserId} import web page from URI '{request.Uri}' in bucket '{request.Bucket}' with parent '{request.ParentId}'");
            Guid id = NewId.NextGuid();

            await _bus.Publish<UploadWebPage>(new
            {
                Id = id,
                request.Bucket,
                Url = request.Uri,
                request.ParentId,
                UserId
            });

            return AcceptedAtRoute("GetSingleEntity", new { type = "files", id = id }, null);
        }
    }
}
