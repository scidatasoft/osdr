using Microsoft.AspNetCore.Mvc;
using Sds.Reflection;
using Serilog;
using System.Reflection;

namespace MetadataStorage.Controllers
{
    [Route("api/[controller]")]
    public class VersionController : Controller
    {
        // GET api/version
        [HttpGet]
        public IActionResult Get()
        {
            Log.Information("Getting version.");
            return Ok(new
            {
                Application = Assembly.GetEntryAssembly().GetTitle(),
                Build = new
                {
                    Version = Assembly.GetEntryAssembly().GetVersion(),
                    TimeStamp = Assembly.GetEntryAssembly().GetBuildTimeStamp(),
                    CommitId = Assembly.GetEntryAssembly().GetCommitId(),
                    CommitAuthor = Assembly.GetEntryAssembly().GetCommitAuthor(),
                }
            });
        }
    }
}
