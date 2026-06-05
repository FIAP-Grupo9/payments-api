using FCG.Payments.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Api.Infrastructure.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.UserEmail).HasMaxLength(255);
            e.Property(x => x.GameName).HasMaxLength(200);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.FailureReason).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(x => x.UserId);
        });

        mb.Entity<ProcessedMessage>(e =>
        {
            e.ToTable("processed_messages");
            e.HasKey(x => x.MessageId);
            e.Property(x => x.MessageType).HasMaxLength(200);
        });
    }
}
