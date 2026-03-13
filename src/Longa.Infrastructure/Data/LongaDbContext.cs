using Longa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Longa.Infrastructure.Data;

public class LongaDbContext : DbContext
{
    public LongaDbContext(DbContextOptions<LongaDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<PushToken> PushTokens => Set<PushToken>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(500);
            e.HasIndex(x => x.Auth0UserId).IsUnique();
            e.Property(x => x.Auth0UserId).HasMaxLength(256);
            e.HasIndex(x => x.IdentifierForVendor).IsUnique();
        });

        modelBuilder.Entity<Trip>(e =>
        {
            e.ToTable("trips",             t => t.HasCheckConstraint("driver_price_display_check",
                "((role = 'driver' AND price_display IS NOT NULL AND price_display != '') OR (role = 'rider'))"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Role)
                .HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => (TripRole)Enum.Parse(typeof(TripRole), v, true))
                .HasMaxLength(20);
            e.Property(x => x.Status)
                .HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => (TripStatus)Enum.Parse(typeof(TripStatus), v, true))
                .HasMaxLength(20)
                .HasDefaultValue(TripStatus.Open);
            e.HasIndex(x => new { x.Role, x.Status });
            e.HasIndex(x => x.DepartureAt);
            e.HasOne(x => x.User).WithMany(u => u.Trips).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.ToTable("bookings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.DriverTripId).IsUnique();
            e.HasIndex(x => x.RiderTripId).IsUnique();
            e.HasOne(x => x.DriverTrip).WithOne(t => t.BookingAsDriver).HasForeignKey<Booking>(x => x.DriverTripId);
            e.HasOne(x => x.RiderTrip).WithOne(t => t.BookingAsRider).HasForeignKey<Booking>(x => x.RiderTripId);
        });

        modelBuilder.Entity<PushToken>(e =>
        {
            e.ToTable("push_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne(u => u.PushToken).HasForeignKey<PushToken>(x => x.UserId);
        });

        modelBuilder.Entity<IdempotencyKey>(e =>
        {
            e.ToTable("idempotency_keys");
            e.HasKey(x => x.Key);
            e.HasIndex(x => x.CreatedAt);
        });
    }
}
