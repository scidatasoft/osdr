using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sds.ChemicalExport.Domain.Commands;
using Sds.Osdr.WebApi.Filters;
using Sds.Osdr.WebApi.Requests;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [UserInfoRequired]
    public class ExportsController : Controller
    {
        private IBusControl _bus;
        IHttpContextAccessor _httpContextAccessor;

        protected Guid UserId => Guid.Parse(User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value);

        public ExportsController(IBusControl bus, IHttpContextAccessor httpContextAccessor) {

            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        [HttpPost("files")]
        public async Task<IActionResult> ExportFile([FromBody]ExportFileRequest request)
        {
            if (request is null)
                return BadRequest();

            Guid exportToBlobId = Guid.NewGuid();

            //var propertyNames = 
            IEnumerable<string> propertyNames = request.Properties.Select(p => p.Name);
            Log.Information($"Exporting file from blob {request.BlobId} into bucket {UserId} with id {exportToBlobId}, format = {request.Format}, properties = [{string.Join(",", propertyNames)}]");
            
            await _bus.Publish<ExportFile>(new
            {
                Bucket = UserId.ToString(),
                Id = exportToBlobId,
                BlobId = request.BlobId,
                Format = request.Format,
                Properties = propertyNames,
                Map = request.Properties.Where(p=>!string.IsNullOrEmpty(p.ExportName)).ToDictionary(p=>p.Name, p=>p.ExportName),
                UserId
            });
            var httpRequest = _httpContextAccessor.HttpContext.Request;
            return Accepted($"{httpRequest.Scheme}://{httpRequest.Host}/blob/v1/api/blobs/{UserId}/{exportToBlobId}");
        }
    }
}
