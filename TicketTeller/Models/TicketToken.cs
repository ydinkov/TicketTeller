namespace TicketTeller.Models;

public class TicketToken
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string? Subject { get; set; }
    public DateTime? ExhaustedDate { get; set; }
    public bool Overage { get; set; }

    // Navigation property
    public Subscription Subscription { get;  }
}