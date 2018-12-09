using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Storage.Blob.WebApi.Swagger
{
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        private readonly IOptions<AuthorizationOptions> authorizationOptions;

        public SecurityRequirementsOperationFilter(IOptions<AuthorizationOptions> authorizationOptions)
        {
            this.authorizationOptions = authorizationOptions;
        }

        public void Apply(Operation operation, OperationFilterContext context)
        {
            var controllerPolicies = context.ApiDescription.ControllerAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.AuthenticationSchemes);
            var actionPolicies = context.ApiDescription.ActionAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.AuthenticationSchemes);
            var policies = controllerPolicies.Union(actionPolicies).Distinct();

            if (policies.Any())
            {
                operation.Responses.Add("401", new Response { Description = "Unauthorized - correct token needed" });

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>();
                operation.Security.Add(
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "Bearer", policies }
                    });
            }
        }
    }
}
