using Microsoft.EntityFrameworkCore;
using TicketTeller.Models;

namespace TicketTeller;

public class YourDbContext : DbContext
{
    public YourDbContext(DbContextOptions<YourDbContext> options) : base(options)
    {
    }

    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<TicketToken> TicketTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>()
            .HasMany(s => s.TicketTokens)
            .WithOne(t => t.Subscription)
            .HasForeignKey(t => t.SubscriptionId);
    }
}