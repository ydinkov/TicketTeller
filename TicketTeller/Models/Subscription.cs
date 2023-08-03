namespace TicketTeller.Models;

public class Subscription
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TimeSpan TicketLifetime { get; set; }
    public string? Metadata { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan RefreshInterval { get; set; }
    public int RefreshAmount { get; set; }
    public DateTime NextRefreshDate { get; set; }

    // Navigation property
    public List<TicketToken> TicketTokens { get; set; } = new ();
}