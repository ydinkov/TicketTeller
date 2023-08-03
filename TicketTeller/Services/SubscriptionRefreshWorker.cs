namespace TicketTeller.Services;

public class SubscriptionRefreshWorker : BackgroundService
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionRefreshWorker(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var subscriptions = await _subscriptionService.GetAllSubscriptions();
            var tasks = subscriptions
                .Where(n => n.NextRefreshDate <= DateTime.UtcNow)
                .Select(x=>_subscriptionService.RefreshTokens(x.Id))
                .ToArray();
            await Task.WhenAll(tasks);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}