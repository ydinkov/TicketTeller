using Microsoft.EntityFrameworkCore;
using TicketTeller.Models;

namespace TicketTeller.Services;

public interface ISubscriptionService
{
    Task<IEnumerable<Subscription>> GetAllSubscriptions();
    Task<Subscription?> GetSubscriptionById(Guid id);
    Task<Subscription> CreateSubscription(Subscription subscription);
    Task<bool> UpdateSubscription(Guid id, Subscription subscription);
    Task<bool> DeleteSubscription(Guid id);
    Task<TicketToken> UseSubscription(Guid subscriptionId, string subject);
    Task<SubscriptionReport> GetSubscriptionReport(Guid subscriptionId);
    Task RefreshTokens(Guid subscriptionId);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly TicketTellerDbContext _dbContext;

    public SubscriptionService(TicketTellerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Subscription>> GetAllSubscriptions()
    {
        return await _dbContext.Subscriptions.ToListAsync();
    }

    public async Task<Subscription?> GetSubscriptionById(Guid id)
    {
        return await _dbContext.Subscriptions.FindAsync(id);
    }

    public async Task<Subscription> CreateSubscription(Subscription subscription)
    {
        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();
        return subscription;
    }

    public async Task<bool> UpdateSubscription(Guid id, Subscription subscription)
    {
        var existingSubscription = await _dbContext.Subscriptions.FindAsync(id);
        if (existingSubscription != null)
        {
            // update fields here based on input subscription object
            await _dbContext.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteSubscription(Guid id)
    {
        var subscriptionToDelete = await _dbContext.Subscriptions.FindAsync(id);
        if (subscriptionToDelete != null)
        {
            _dbContext.Subscriptions.Remove(subscriptionToDelete);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<TicketToken> UseSubscription(Guid subscriptionId, string subject)
    {
        var subscription = await _dbContext.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
        {
            throw new ArgumentException("Invalid subscription ID.");
        }

        var ticket = subscription.TicketTokens
            .Where(t => t.Subject == null && t.ExhaustedDate == null && t.ExpirationDate > DateTime.UtcNow)
            .MinBy(t => t.ExpirationDate);

        if (ticket != null)
        {
            ticket.Subject = subject;
            ticket.ExhaustedDate = DateTime.UtcNow;
        }
        else
        {
            ticket = new TicketToken
            {
                SubscriptionId = subscriptionId,
                CreationDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.Add(subscription.TicketLifetime),
                ExhaustedDate = DateTime.UtcNow,
                Subject = subject,
                Overage = true
            };

            subscription.TicketTokens.Add(ticket);
        }

        await _dbContext.SaveChangesAsync();
        return ticket;
    }

    public async Task<SubscriptionReport> GetSubscriptionReport(Guid subscriptionId)
    {
        var subscription = await _dbContext.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
        {
            throw new ArgumentException("Invalid subscription ID.");
        }

        var exhaustedTicketsCount = subscription.TicketTokens.Count(t => t.ExhaustedDate != null);
        var expiredButNotExhaustedTicketsCount =
            subscription.TicketTokens.Count(t => t.ExpirationDate < DateTime.UtcNow && t.ExhaustedDate == null);
        var overageTicketsCount = subscription.TicketTokens.Count(t => t.Overage);

        var report = new SubscriptionReport
        {
            ExhaustedTicketsCount = exhaustedTicketsCount,
            ExpiredButNotExhaustedTicketsCount = expiredButNotExhaustedTicketsCount,
            OverageTicketsCount = overageTicketsCount
        };

        return report;
    }

    public async Task RefreshTokens(Guid subscriptionId)
    {
        var subscription = await _dbContext.Subscriptions
            .Include(s => s.TicketTokens)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);

        if (subscription == null)
        {
            throw new Exception("Subscription not found");
        }

        var now = DateTime.UtcNow;

        if (subscription.NextRefreshDate <= now)
        {
            for (int i = 0; i < subscription.RefreshAmount; i++)
            {
                var ticketToken = new TicketToken
                {
                    SubscriptionId = subscription.Id,
                    CreationDate = now,
                    ExpirationDate = now + subscription.TicketLifetime,
                    Overage = false
                };

                subscription.TicketTokens.Add(ticketToken);
            }

            subscription.NextRefreshDate = now + subscription.RefreshInterval;

            _dbContext.Update(subscription); // Explicitly telling EF Core that the subscription entity has been modified
            await _dbContext.SaveChangesAsync();
        }
    }
}