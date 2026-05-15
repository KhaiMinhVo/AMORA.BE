using System.Net;

namespace Amora.Application.Exceptions;

public sealed class ForbiddenApiException : ApiException
{
    public ForbiddenApiException(string message = "Forbidden", string errorCode = "forbidden")
        : base(message, HttpStatusCode.Forbidden, errorCode)
    {
    }
}