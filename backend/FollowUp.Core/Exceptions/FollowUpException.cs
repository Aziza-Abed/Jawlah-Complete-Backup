namespace FollowUp.Core.Exceptions;

public class FollowUpException : Exception
{
    public FollowUpException()
    {
    }

    public FollowUpException(string message) : base(message)
    {
    }

    public FollowUpException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class NotFoundException : FollowUpException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }
}

public class ValidationException : FollowUpException
{
    public ValidationException(string message) : base(message)
    {
    }
}

public class UnauthorizedException : FollowUpException
{
    public UnauthorizedException(string message = "Unauthorized access.") : base(message)
    {
    }
}
