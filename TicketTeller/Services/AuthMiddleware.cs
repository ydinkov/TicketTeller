namespace TicketTeller.Services;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private const string APIKEYNAME = "ApiKey";

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Api Key was not provided.");
            return;
        }

        var adminApiKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
        var contributorApiKey = Environment.GetEnvironmentVariable("CONTRIBUTOR_API_KEY");
        var userApiKey = Environment.GetEnvironmentVariable("USER_API_KEY");

        if (adminApiKey != null && adminApiKey.Equals(extractedApiKey))
        {
            context.Items["Role"] = "Admin";
            await _next(context);
        }
        else if (contributorApiKey != null && contributorApiKey.Equals(extractedApiKey))
        {
            context.Items["Role"] = "Contributor";
            await _next(context);
        }
        else if (userApiKey != null && userApiKey.Equals(extractedApiKey))
        {
            context.Items["Role"] = "User";
            await _next(context);
        }
        else
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync($"Invalid Api Key provided.");
            return;
        }
    }
}
