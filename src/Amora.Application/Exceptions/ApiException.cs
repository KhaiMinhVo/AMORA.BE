using System.Net;

namespace Amora.Application.Exceptions;

public abstract class ApiException : Exception
{
    protected ApiException(string message, HttpStatusCode statusCode, string errorCode) : base(message)
    {
        StatusCode = (int)statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }

    public string ErrorCode { get; }
}