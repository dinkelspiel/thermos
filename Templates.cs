namespace thermos;

internal static class Templates {
    public static string InvalidRoute = 
        "<h1>Not Found</h1><br/>" +
        "The requested route was not found on this server.";

    public static string InvalidMethod = 
        "<h1>Invalid Method</h1><br/>" +
        "The requested route doesn't support the given method.";
}   