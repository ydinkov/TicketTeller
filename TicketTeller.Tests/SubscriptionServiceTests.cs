public class SubscriptionServiceTest
{
    private ISubscriptionService _subscriptionService;
    private TicketTellerDbContext _dbContext;

    public SubscriptionServiceTest()
    {
        var options = new DbContextOptionsBuilder<TicketTellerDbContext>()
            .UseInMemoryDatabase(databaseName: "testDatabase")
            .Options;

        _dbContext = new TicketTellerDbContext(options);
        _subscriptionService = new SubscriptionService(_dbContext);
    }

    [Fact]
    public async Task GetAllSubscriptions_ReturnsAllSubscriptions()
    {
        // Arrange
        var expectedSubscription = new Subscription { Id = Guid.NewGuid(), Name = "Test Subscription", Description = "This is a test subscription." };
        _dbContext.Subscriptions.Add(expectedSubscription);
        await _dbContext.SaveChangesAsync();

        // Act
        var subscriptions = await _subscriptionService.GetAllSubscriptions();

        // Assert
        subscriptions.Should().Contain(s => s.Id == expectedSubscription.Id);
    }

    [Fact]
    public async Task GetSubscriptionById_ReturnsSubscription_WhenIdExists()
    {
        // Arrange
        var expectedSubscription = new Subscription { Id = Guid.NewGuid(), Name = "Test Subscription", Description = "This is a test subscription." };
        _dbContext.Subscriptions.Add(expectedSubscription);
        await _dbContext.SaveChangesAsync();

        // Act
        var subscription = await _subscriptionService.GetSubscriptionById(expectedSubscription.Id);

        // Assert
        subscription.Should().NotBeNull();
        subscription?.Id.Should().Be(expectedSubscription.Id);
    }

    [Fact]
    public async Task GetSubscriptionById_ReturnsNull_WhenIdDoesNotExist()
    {
        // Arrange
        var nonExistentGuid = Guid.NewGuid();

        // Act
        var subscription = await _subscriptionService.GetSubscriptionById(nonExistentGuid);

        // Assert
        subscription.Should().BeNull();
    }
    [Fact]
    public async Task UseSubscription_ShouldReturnTicket_WhenCalledWithValidSubscriptionIdAndSubject()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Test Subscription",
            Description = "This is a test subscription.",
            TicketLifetime = TimeSpan.FromHours(1),
            TicketTokens = new List<TicketToken>
            {
                new TicketToken
                {
                    Id = Guid.NewGuid(),
                    ExpirationDate = DateTime.UtcNow.AddHours(1)
                }
            }
        };
        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        // Act
        var ticket = await _subscriptionService.UseSubscription(subscription.Id, "Test Subject");

        // Assert
        ticket.Should().NotBeNull();
        ticket.Subject.Should().Be("Test Subject");
        ticket.ExhaustedDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UseSubscription_ShouldThrowArgumentException_WhenCalledWithInvalidSubscriptionId()
    {
        // Arrange
        var invalidSubscriptionId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _subscriptionService.UseSubscription(invalidSubscriptionId, "Test Subject");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GetSubscriptionReport_ShouldReturnCorrectReport_WhenCalledWithValidSubscriptionId()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Test Subscription",
            Description = "This is a test subscription.",
            TicketLifetime = TimeSpan.FromHours(1),
            TicketTokens = new List<TicketToken>
            {
                new TicketToken
                {
                    Id = Guid.NewGuid(),
                    ExpirationDate = DateTime.UtcNow.AddHours(1),
                    ExhaustedDate = DateTime.UtcNow
                },
                new TicketToken
                {
                    Id = Guid.NewGuid(),
                    ExpirationDate = DateTime.UtcNow.AddHours(-1),
                    ExhaustedDate = null
                },
                new TicketToken
                {
                    Id = Guid.NewGuid(),
                    ExpirationDate = DateTime.UtcNow.AddHours(1),
                    ExhaustedDate = DateTime.UtcNow,
                    Overage = true
                }
            }
        };
        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        // Act
        var report = await _subscriptionService.GetSubscriptionReport(subscription.Id);

        // Assert
        report.Should().NotBeNull();
        report.ExhaustedTicketsCount.Should().Be(2);
        report.ExpiredButNotExhaustedTicketsCount.Should().Be(1);
        report.OverageTicketsCount.Should().Be(1);
    }
    
    [Fact]
    public async Task UseSubscriptionAndReport_ShouldCorrectlyHandleTicketsAndOverages_WhenCalledMultipleTimes()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Test Subscription",
            Description = "This is a test subscription.",
            TicketLifetime = TimeSpan.FromHours(1),
            TicketTokens = new List<TicketToken>
            {
                new TicketToken
                {
                    Id = Guid.NewGuid(),
                    ExpirationDate = DateTime.UtcNow.AddHours(1),
                    ExhaustedDate = null,
                    Subject = null
                },
                new TicketToken
                {
                    Id = Guid.NewGuid(),
                    ExpirationDate = DateTime.UtcNow.AddHours(1),
                    ExhaustedDate = null,
                    Subject = null
                }
            }
        };
        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        // Act
        // Use first token
        await _subscriptionService.UseSubscription(subscription.Id, "first usage");
        // Use second token
        await _subscriptionService.UseSubscription(subscription.Id, "second usage");
        // Create an overage token
        await _subscriptionService.UseSubscription(subscription.Id, "overage usage");
    
        var report = await _subscriptionService.GetSubscriptionReport(subscription.Id);

        // Assert
        report.ExhaustedTicketsCount.Should().Be(3);
        report.OverageTicketsCount.Should().Be(1);
    }

    [Fact]
    public async Task UseTokensUntilOverage_ReportsCorrectly()
    {
        // Arrange
        var subscriptionRequest = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = "Test Subscription",
            Description = "This is a test subscription.",
            TicketLifetime = TimeSpan.FromHours(1),
            RefreshAmount = 2,
            RefreshInterval = TimeSpan.FromHours(1),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(1),
            NextRefreshDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)) // Set to a time in the past
        };

        var subscription = await _subscriptionService.CreateSubscription(subscriptionRequest);

        // Act
        await _subscriptionService.RefreshTokens(subscription.Id);
        await _subscriptionService.UseSubscription(subscription.Id, "Test1");
        await _subscriptionService.UseSubscription(subscription.Id, "Test2");
        await _subscriptionService.UseSubscription(subscription.Id, "Test3"); // This will create an overage token

        // Now, let's get the report
        var report = await _subscriptionService.GetSubscriptionReport(subscription.Id);

        // Assert
        report.ExhaustedTicketsCount.Should().Be(3);
        report.ExpiredButNotExhaustedTicketsCount.Should().Be(0);
        report.OverageTicketsCount.Should().Be(1);
    }


}
