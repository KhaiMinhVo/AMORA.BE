using System.Text.Json;
using Amora.Application.Common;
using Amora.Application.Exceptions;

namespace Amora.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            await WriteResponseAsync(context, ex.StatusCode, ex.Message, ex.ErrorCode);
        }
        catch (InvalidOperationException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status400BadRequest, ex.Message, "bad_request");
        }
        catch (Exception)
        {
            await WriteResponseAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "server_error");
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string message, string errorCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<object>.Fail(message, errorCode);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}