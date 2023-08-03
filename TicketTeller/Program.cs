using Microsoft.EntityFrameworkCore;
using TicketTeller;
using TicketTeller.Models;
using TicketTeller.Services; // Include your service namespace

var builder = WebApplication.CreateBuilder(args);

// Add database context and service to DI container
builder.Services.AddDbContext<TicketTellerDbContext>(options =>
{
    string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
    switch (Enum.Parse<EDBType>(Environment.GetEnvironmentVariable("DB_TYPE") ?? "postgres"))
    {
        case EDBType.sqlserver:
            options.UseSqlServer(connectionString);
            break;
        case EDBType.postgres:
            options.UseNpgsql(connectionString);
            break;
        case EDBType.mysql:
            options.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
            break;
        case EDBType.sqlite:
            options.UseSqlite(connectionString);
            break;
        default:
            throw new ArgumentOutOfRangeException();
    };
});

// Register the subscription service
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddHostedService<SubscriptionRefreshWorker>();

var app = builder.Build();
app.UseMiddleware<AuthMiddleware>();
// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var dbContext = services.GetRequiredService<TicketTellerDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.MapGet("/subscriptions", async (ISubscriptionService service) => 
    await service.GetAllSubscriptions());

app.MapGet("/subscriptions/{id}", async (ISubscriptionService service, Guid id) =>
    await service.GetSubscriptionById(id) is Subscription s ? Results.Ok(s) : Results.NotFound());

app.MapPost("/subscriptions", async (ISubscriptionService service, Subscription s) =>
{
    var createdSubscription = await service.CreateSubscription(s);
    return Results.Created($"/subscriptions/{createdSubscription.Id}", createdSubscription);
});

app.MapPut("/subscriptions/{id}", async (ISubscriptionService service, Guid id, Subscription s) => 
    await service.UpdateSubscription(id, s) ? Results.Ok() : Results.NotFound());

app.MapDelete("/subscriptions/{id}", async (ISubscriptionService service, Guid id) => 
    await service.DeleteSubscription(id) ? Results.Ok() : Results.NotFound());

app.MapPost("/subscriptions/{id}/use", async (ISubscriptionService service, Guid id, string subject) => 
    await service.UseSubscription(id, subject));

app.MapGet("/subscriptions/{id}/report", async (ISubscriptionService subscriptionService, Guid id) =>
{
    try
    {
        var report = await subscriptionService.GetSubscriptionReport(id);
        return Results.Ok(report);
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }
});

app.Run();
