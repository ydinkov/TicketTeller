namespace TicketTeller.Models;

public class SubscriptionReport
{
    public int ExhaustedTicketsCount { get; set; }
    public int ExpiredButNotExhaustedTicketsCount { get; set; }
    public int OverageTicketsCount { get; set; }
}
