using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Sds.Imaging.WebApi.Swagger
{
    public class AddUploadFileParameter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (string.Equals(context.ApiDescription.RelativePath, "api/files", StringComparison.OrdinalIgnoreCase))
            {
                var imageParameters = new IParameter[]
                {
                    new BodyParameter
                    {
                        Name = "request", // must match parameter name from controller method
                        In = "formData",
                        Description = "Image generation options",
                        Schema = new Schema { Type = "array", Items = new Schema { Ref = "#/definitions/ImageRequest"} }
                    },
                    new NonBodyParameter
                    {
                        Name = "File", // must match parameter name from controller method
                        In = "formData",
                        Description = "Upload file.",
                        Required = true,
                        Type = "file"
                    }
                }.ToList();

                if (operation.Parameters is null)
                {
                    operation.Parameters = imageParameters;
                }
                else
                {
                    imageParameters.AddRange(operation.Parameters);
                    operation.Parameters = imageParameters;
                }

                operation.Consumes.Add("application/form-data");
            }
        }
    }
}
