namespace Amora.Application.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public T? Data { get; init; }

    public string? ErrorCode { get; init; }

    public static ApiResponse<T> Ok(T? data, string? message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            ErrorCode = null
        };
    }

    public static ApiResponse<T> Fail(string message, string errorCode)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            ErrorCode = errorCode
        };
    }
}