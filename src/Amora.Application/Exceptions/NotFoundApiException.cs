using System.Net;

namespace Amora.Application.Exceptions;

public sealed class NotFoundApiException : ApiException
{
    public NotFoundApiException(string message, string errorCode = "not_found")
        : base(message, HttpStatusCode.NotFound, errorCode)
    {
    }
}