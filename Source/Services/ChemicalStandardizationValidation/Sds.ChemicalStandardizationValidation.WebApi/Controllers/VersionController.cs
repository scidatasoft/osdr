using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog;

namespace Sds.ChemicalStandardizationValidation.WebApi.Controllers
{
    [Route("api/version")]
    public class VersionController : Controller
    {
        // GET api/version
        [HttpGet(Name = "GetVersion")]
        public IActionResult Get()
        {
            Log.Debug("Getting version.");
            return Ok($"{PlatformServices.Default.Application.ApplicationName} v{PlatformServices.Default.Application.ApplicationVersion}");
        }
    }
}