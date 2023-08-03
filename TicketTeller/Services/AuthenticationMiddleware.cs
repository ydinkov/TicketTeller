using Microsoft.AspNetCore.Authorization;

namespace TicketTeller.Services;

public class RoleRequirement : IAuthorizeData
{
    public string? Policy { get; set; }
    public string? Roles { get; set; }
    public string? Scheme { get; set; }
    public string? AuthenticationSchemes { get; set; }

    public RoleRequirement(string role)
    {
        Roles = role;
    }
}

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string APIKEYNAME = "ApiKey";

    public AuthenticationMiddleware(RequestDelegate next)
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

public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        var roleRequirement = endpoint.Metadata.GetMetadata<IAuthorizeData>();

        var gotRole = !context.Items.TryGetValue("Role", out var role);
        if (roleRequirement != null && gotRole)
        {
            context.Response.StatusCode = 401;
            return;
        }

        if (roleRequirement != null && role?.ToString() != roleRequirement.Roles)
        {
            context.Response.StatusCode = 403;
            return;
        }

        await _next(context);
    }
}