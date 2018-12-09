using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Extensions
{
    public sealed class FileCallbackResultExecutor : FileResultExecutorBase
    {
        public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
        {
        }

        public Task ExecuteAsync(ActionContext context, FileCallbackResult result)
        {
            SetHeadersAndLog(context, result, null, false);
            return result.Callback(context.HttpContext.Response.Body, context);
        }
    }
}
