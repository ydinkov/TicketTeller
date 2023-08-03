using Microsoft.EntityFrameworkCore;
using TicketTeller;
using TicketTeller.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Registering DB Context
builder.Services.AddDbContext<YourDbContext>(options =>
{
    string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
    switch (Enum.Parse<EDBType>(Environment.GetEnvironmentVariable("DB_TYPE") ?? "sqlserver"))
    {
        case EDBType.sqlserver:
            options.UseSqlServer(connectionString);
            break;
        case EDBType.postgres:
            options.UseNpgsql(connectionString);
            break;
        case EDBType.mysql:
            options.UseMySql(connectionString,MySqlServerVersion.LatestSupportedServerVersion);
            break;
        default:
            throw new ArgumentOutOfRangeException();
    };
    
    
});

app.MapGet("/subscriptions", async (YourDbContext db) => 
    await db.Subscriptions.ToListAsync());

app.MapGet("/subscriptions/{id}", async (YourDbContext db, Guid id) =>
    await db.Subscriptions.FindAsync(id) is Subscription s ? Results.Ok(s) : Results.NotFound());

app.MapPost("/subscriptions", async (YourDbContext db, Subscription s) =>
{
    db.Subscriptions.Add(s);
    await db.SaveChangesAsync();
    return Results.Created($"/subscriptions/{s.Guid}", s);
});

app.MapPut("/subscriptions/{id}", async (YourDbContext db, Guid id, Subscription s) =>
{
    var subscriptionToUpdate = await db.Subscriptions.FindAsync(id);
    if (subscriptionToUpdate != null)
    {
        subscriptionToUpdate = s; // might want to adjust this to only update specific fields
        await db.SaveChangesAsync();
        return Results.Ok();
    }
    return Results.NotFound();
});

app.MapDelete("/subscriptions/{id}", async (YourDbContext db, Guid id) =>
{
    var subscriptionToDelete = await db.Subscriptions.FindAsync(id);
    if (subscriptionToDelete != null)
    {
        db.Subscriptions.Remove(subscriptionToDelete);
        await db.SaveChangesAsync();
        return Results.Ok();
    }
    return Results.NotFound();
});

app.MapPost("/subscriptions/{id}/use", async (YourDbContext db, Guid id, string subject) =>
{
    var subscription = await db.Subscriptions.FindAsync(id);
    if (subscription != null)
    {
        var ticket = subscription.TicketTokens
            .Where(t => t.Subject == null && t.ExhaustedDate == null && t.ExpirationDate > DateTime.Now)
            .MinBy(t => t.ExpirationDate);

        if (ticket != null)
        {
            ticket.Subject = subject;
            ticket.ExhaustedDate = DateTime.Now;
            await db.SaveChangesAsync();
        }
        else
        {
            var newTicket = new TicketToken
            {
                SubscriptionId = id,
                CreationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.Add(subscription.TicketLifetime),
                ExhaustedDate = DateTime.Now,
                Subject = subject,
                Overage = true
            };
            subscription.TicketTokens.Add(newTicket);
            await db.SaveChangesAsync();
        }

        return Results.Ok();
    }
    return Results.NotFound();
});

app.Run();
