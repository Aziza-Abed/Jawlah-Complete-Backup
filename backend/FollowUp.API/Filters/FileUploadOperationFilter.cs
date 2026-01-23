using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FollowUp.API.Filters;

// swagger operation filter to properly handle IFormFile parameters in API documentation
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // find all [FromForm] parameters
        var formParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source.Id == "Form")
            .ToList();

        if (!formParameters.Any())
            return;

        // check if any parameter is IFormFile (including nullable)
        var hasFileParameter = formParameters.Any(p =>
            p.Type == typeof(IFormFile) ||
            Nullable.GetUnderlyingType(p.Type) == typeof(IFormFile));

        // if there's no file, we don't need to do anything special
        if (!hasFileParameter)
            return;

        // build the multipart/form-data schema
        var properties = new Dictionary<string, OpenApiSchema>();
        foreach (var param in formParameters)
        {
            var underlyingType = Nullable.GetUnderlyingType(param.Type) ?? param.Type;

            if (underlyingType == typeof(IFormFile))
            {
                properties[param.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Nullable = param.Type != underlyingType
                };
            }
            else
            {
                properties[param.Name] = new OpenApiSchema
                {
                    Type = GetSchemaType(param.Type),
                    Nullable = param.Type.IsGenericType &&
                              param.Type.GetGenericTypeDefinition() == typeof(Nullable<>)
                };
            }
        }

        // replace operation parameters with request body
        operation.Parameters.Clear();
        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties
                    }
                }
            }
        };
    }

    private string GetSchemaType(Type type)
    {
        // handle nullable types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type)!;
        }

        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long)) return "integer";
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(DateTime)) return "string";
        return "string";
    }
}
