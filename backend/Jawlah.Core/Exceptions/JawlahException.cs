namespace Jawlah.Core.Exceptions;

public class JawlahException : Exception
{
    public JawlahException()
    {
    }

    public JawlahException(string message) : base(message)
    {
    }

    public JawlahException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class NotFoundException : JawlahException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }
}

public class ValidationException : JawlahException
{
    public ValidationException(string message) : base(message)
    {
    }
}

public class UnauthorizedException : JawlahException
{
    public UnauthorizedException(string message = "Unauthorized access.") : base(message)
    {
    }
}
