using Microsoft.EntityFrameworkCore;
using Notifications.Domain;

namespace Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(b =>
        {
            b.ToTable("notifications");

            b.HasKey(x => x.Id);

            b.Property(x => x.Channel).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            b.Property(x => x.Recipient).IsRequired().HasMaxLength(320);
            b.Property(x => x.RecipientTimeZone).IsRequired().HasMaxLength(128);

            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Body).IsRequired().HasMaxLength(4000);

            b.Property(x => x.ScheduledAtUtc).IsRequired();
            b.Property(x => x.Attempts).IsRequired();

            b.Property(x => x.LastError).HasMaxLength(2000);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasIndex(x => new { x.Status, x.ScheduledAtUtc });
        });
    }
}
