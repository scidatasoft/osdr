using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Extensions
{
    public class FileCallbackResult : FileResult
    {
        public readonly Func<Stream, ActionContext, Task> Callback;

        public FileCallbackResult(MediaTypeHeaderValue contentType, Func<Stream, ActionContext, Task> callback)
            : base(contentType?.ToString())
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            Callback = callback;
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var executor = context.HttpContext.RequestServices.GetRequiredService<FileCallbackResultExecutor>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
