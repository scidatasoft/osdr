using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using MongoDB.Driver;
using System;
using System.Linq;
using Sds.Osdr.WebApi.DataProviders;

namespace Sds.Osdr.WebApi.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowSharedContentAttribute : Attribute, IResourceFilter, IAllowAnonymous
    {
        public void OnResourceExecuted(ResourceExecutedContext context)
        {

        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;

            if (controllerActionDescriptor != null)
            {
                Guid? currentUserId = null;
                var parameterId = context.RouteData.Values["id"];
                if (context. HttpContext.User.Claims.Any(c => c.Type.Contains("/nameidentifier")))
                {
                    currentUserId = Guid.Parse(context.HttpContext.User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value);
                    if (controllerActionDescriptor.AttributeRouteInfo.Name == "GetNodesInRoot")
                        parameterId = currentUserId;
                }
                Guid entityId;
                
                if (parameterId == null)
                {
                    context.Result = new BadRequestResult();
                    return;
                }

                Guid.TryParse(parameterId.ToString(), out entityId);
                var organize = context.HttpContext.RequestServices.GetService<IOrganizeDataProvider>();

                if (!organize.IsItemAccessible(entityId, currentUserId))
                    context.Result = new ForbidResult();
            }
        }
    }
}