namespace thermos;

public class InvalidResponseTypeException : Exception
{
    public InvalidResponseTypeException(string route, string type) : base(String.Format("Route '{0}' has an invalid response type: {1}!", route, type)) {}
}

public class MethodAlreadyDefinedException : Exception
{
    public MethodAlreadyDefinedException(string route, string method) : base(String.Format("Route '{0}' already defines method: {1}!")) {}
}

public class ConflictingRoutesException : Exception 
{
    public ConflictingRoutesException(string route) : base(String.Format("The provided route '{0}' has multiple conflicting options.", route)) {}
}