using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.Extensions.PlatformAbstractions;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Sds.ChemicalProperties.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class VersionController : Controller
    {
        // GET api/version
        [HttpGet]
        public IActionResult Get()
        {
            Log.Information("Getting version.");
            return Ok($"{PlatformServices.Default.Application.ApplicationName} v{PlatformServices.Default.Application.ApplicationVersion}");
        }
    }
}
