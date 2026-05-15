using System.Net;

namespace Amora.Application.Exceptions;

public sealed class ValidationApiException : ApiException
{
    public ValidationApiException(string message, string errorCode = "validation_error")
        : base(message, HttpStatusCode.BadRequest, errorCode)
    {
    }
}