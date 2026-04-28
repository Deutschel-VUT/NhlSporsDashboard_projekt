namespace SportsDashboard.Exceptions;

public class ApiException : Exception
{
    public int? StatusCode { get; }

    public ApiException(string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}

public class ApiTimeoutException : ApiException
{
    public ApiTimeoutException(string message, Exception? inner = null)
        : base(message, null, inner) { }
}

public class ApiUnavailableException : ApiException
{
    public ApiUnavailableException(string message, Exception? inner = null)
        : base(message, 503, inner) { }
}

public class DataParseException : Exception
{
    public DataParseException(string message, Exception? inner = null)
        : base(message, inner) { }
}
