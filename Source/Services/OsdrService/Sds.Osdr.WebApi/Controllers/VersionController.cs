using Microsoft.AspNetCore.Mvc;
using Sds.Reflection;
using System.Reflection;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class VersionController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Application = Assembly.GetEntryAssembly().GetTitle(),
                Build = new
                {
                    Version = Assembly.GetEntryAssembly().GetVersion(),
                    TimeStamp = Assembly.GetEntryAssembly().GetBuildTimeStamp(),
                    CommitId = Assembly.GetEntryAssembly().GetCommitId(),
                    CommitAuthor = Assembly.GetEntryAssembly().GetCommitAuthor(),
                    Hosting = System.Runtime.InteropServices.RuntimeInformation.OSDescription
                }
            });
        }
    }
}
