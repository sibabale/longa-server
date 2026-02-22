using System.ComponentModel.DataAnnotations.Schema;

namespace Longa.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid DriverTripId { get; set; }
    public Guid RiderTripId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    [ForeignKey(nameof(DriverTripId))]
    public Trip DriverTrip { get; set; } = null!;
    [ForeignKey(nameof(RiderTripId))]
    public Trip RiderTrip { get; set; } = null!;
}
