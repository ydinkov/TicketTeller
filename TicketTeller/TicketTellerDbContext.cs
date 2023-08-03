using Microsoft.EntityFrameworkCore;
using TicketTeller.Models;

namespace TicketTeller;

public class TicketTellerDbContext : DbContext
{
    public TicketTellerDbContext(DbContextOptions<TicketTellerDbContext> options) : base(options)
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

        modelBuilder.Entity<Subscription>().HasKey(x => x.Id);
        
        modelBuilder.Entity<TicketToken>().HasKey(x => x.Id);
    }
}