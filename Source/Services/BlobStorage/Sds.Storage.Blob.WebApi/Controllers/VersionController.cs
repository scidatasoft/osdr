using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Sds.Reflection;

namespace Sds.Blob.Storage.WebApi
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class VersionController : Controller
    {
        /// <summary>
        /// Get blobs api version
        /// </summary>
        /// <response code="200">Success - version returned</response>
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
