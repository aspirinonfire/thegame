using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TheGame.Api;

public class SwaggerCsrfTokenOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var methodDoesNotRequireAntiforgery = context.ApiDescription.HttpMethod == "HEAD" ||
      context.ApiDescription.HttpMethod == "TRACE" ||
      context.ApiDescription.HttpMethod == "OPTIONS" ||
      context.ApiDescription.HttpMethod == "GET";

    if (methodDoesNotRequireAntiforgery)
    {
      return;
    }

    if (context.ApiDescription.RelativePath?.StartsWith("/account") ?? false)
    {
      return;
    }

    operation.Parameters.Add(new OpenApiParameter
    {
      Name = "X-XSRF-TOKEN",
      In = ParameterLocation.Header,
      Description = "Antiforgery Token. Can be obtained by calling GET /api/xsrftoken endpoint.",
      Required = true,
      Schema = new OpenApiSchema
      {
        Type = "string"
      }
    });
  }
}
