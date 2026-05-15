using System.Net;

namespace Amora.Application.Exceptions;

public sealed class ConflictApiException : ApiException
{
    public ConflictApiException(string message, string errorCode = "conflict")
        : base(message, HttpStatusCode.Conflict, errorCode)
    {
    }
}