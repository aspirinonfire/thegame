using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TheGame.Domain.Utils;

namespace TheGame.Api.Common;

public static class EndpointHelpers
{
  /// <summary>
  /// Transform <see cref="Result{FromSuccess}"/> to Http <see cref="Result{ToSuccess}"/>
  /// </summary>
  /// <typeparam name="FromSuccess"></typeparam>
  /// <typeparam name="ToSuccess"></typeparam>
  /// <param name="fromResult"></param>
  /// <param name="transformer"></param>
  /// <returns></returns>
  public static Result<ToSuccess> TransformTo<FromSuccess, ToSuccess>(this Result<FromSuccess> fromResult, Func<FromSuccess, ToSuccess> transformer) =>
    fromResult.TryGetSuccessful(out var success, out var failure) ?
      transformer(success) :
      failure;

  /// <summary>
  /// Transform <see cref="Result{FromSuccess}"/> to Http <see cref="Result{ToSuccess}"/>
  /// </summary>
  /// <typeparam name="FromSuccess"></typeparam>
  /// <typeparam name="ToSuccess"></typeparam>
  /// <param name="fromResultTask"></param>
  /// <param name="transformer"></param>
  /// <returns></returns>
  public static async Task<Result<ToSuccess>> TransformTo<FromSuccess, ToSuccess>(this Task<Result<FromSuccess>> fromResultTask, Func<FromSuccess, ToSuccess> transformer) =>
    (await fromResultTask).TransformTo(transformer);

  /// <summary>
  /// Convert <see cref="Result{TSuccess}"/> to Http <see cref="IResult"/>
  /// </summary>
  /// <remarks>
  /// <list type="bullet">
  ///     <item>Success - 200</item>
  ///     <item>Validation Failure - 400</item>
  ///     <item>Other Failures - 500</item>
  /// </list>
  /// </remarks>
  /// <typeparam name="TSuccess"></typeparam>
  /// <param name="result"></param>
  /// <param name="httpContext"></param>
  /// <returns></returns>
  public static IResult ToHttpResponse<TSuccess>(this Result<TSuccess> result, HttpContext httpContext)
  {
    var correlationId = httpContext.RetrieveCorrelationId();

    if (result.TryGetSuccessful(out var success, out var failure))
    {
      return TypedResults.Ok(success);
    }
    // TODO support validation failures
    //else if (failure.TryGetValidationFailure(out var validationFailure))
    //{
    //  return TypedResults.ValidationProblem(
    //    errors: validationFailure.ValidationFailures,
    //    detail: "Please correct the errors and try again.",
    //    extensions: new Dictionary<string, object?>()
    //    {
    //      [GameApiMiddleware.CorrelationIdKey] = correlationId
    //    });
    //}
    else
    {
      return Results.Problem(
        title: "An error occurred while processing your request.",
        statusCode: (int)HttpStatusCode.InternalServerError,
        detail: failure.ErrorMessage,
        extensions: new Dictionary<string, object?>()
        {
          [GameApiMiddleware.CorrelationIdKey] = correlationId
        });
    }
  }

  /// <summary>
  /// Convert <see cref="Result{TSuccess}"/> to Http <see cref="IResult"/>
  /// </summary>
  /// <remarks>
  /// <list type="bullet">
  ///     <item>Success - 200</item>
  ///     <item>Validation Failure - 400</item>
  ///     <item>Other Failures - 500</item>
  /// </list>
  /// </remarks>
  /// <typeparam name="TSuccess"></typeparam>
  /// <param name="resultTask"></param>
  /// <param name="httpContext"></param>
  /// <returns></returns>
  public static async Task<IResult> ToHttpResponse<TSuccess>(this Task<Result<TSuccess>> resultTask, HttpContext httpContext) => (await resultTask)
    .ToHttpResponse(httpContext);
}
