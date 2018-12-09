using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Net;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class ProxyController : Controller
    {
        public ProxyController() { }

        [HttpGet("{jsmol}")]
        public async Task<IActionResult> Import(string call, string database, string query)
        {
            var webClient = new WebClient();
            var uriContent = await webClient.OpenReadTaskAsync($"https://chemapps.stolaf.edu/jmol/jsmol/php/jsmol.php?call={call}&database={database}&query={WebUtility.UrlEncode(query)}");

            return File(uriContent, webClient.ResponseHeaders["Content-Type"] ?? "application/octet-stream");
        }
    }
}
