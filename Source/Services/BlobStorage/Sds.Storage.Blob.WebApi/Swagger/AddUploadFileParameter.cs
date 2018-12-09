using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace Sds.Storage.Blob.WebApi.Swagger
{
    public class AddUploadFileParameter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (string.Equals(context.ApiDescription.RelativePath ,"api/blobs/{bucket}", StringComparison.OrdinalIgnoreCase))
            {
                operation.Parameters.Add(new BodyParameter
                {
                    Name = "Metadata", // must match parameter name from controller method
                    In = "formData",
                    Description = "Additional info associated with file",
                    //Type = "string"
                });

                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "File", // must match parameter name from controller method
                    In = "formData",
                    Description = "Upload file.",
                    Required = true,
                    Type = "file"
                });

                operation.Consumes.Add("application/form-data");
            }
        }
    }
}
