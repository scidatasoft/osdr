using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace Sds.Osdr.WebApi.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UserInfoRequiredAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuted(ResourceExecutedContext context)
        {

        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                var isAllowAnonymous = controllerActionDescriptor.MethodInfo.GetCustomAttributes(inherit: true)
                    .Any(a => typeof(IAllowAnonymous).IsAssignableFrom(a.GetType()));

                if (!context.HttpContext.User.Claims.Any(c => c.Type.Contains("/nameidentifier")) && !isAllowAnonymous)
                    context.Result = new BadRequestResult();
            }
        }
    }
}