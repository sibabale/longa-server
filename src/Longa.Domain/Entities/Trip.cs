using System.ComponentModel.DataAnnotations.Schema;

namespace Longa.Domain.Entities;

public class Trip
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TripRole Role { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Open;

    public required string PickupAddress { get; set; }
    public decimal PickupLat { get; set; }
    public decimal PickupLng { get; set; }
    public required string DestinationAddress { get; set; }
    public decimal DestinationLat { get; set; }
    public decimal DestinationLng { get; set; }

    public DateTimeOffset DepartureAt { get; set; }
    public string? PriceDisplay { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    public Booking? BookingAsDriver { get; set; }
    public Booking? BookingAsRider { get; set; }
}
