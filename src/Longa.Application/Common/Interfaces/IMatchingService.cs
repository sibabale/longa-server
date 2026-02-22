using Longa.Domain.Entities;

namespace Longa.Application.Common.Interfaces;

public interface IMatchingService
{
    Task<Booking?> TryMatchDriverTripAsync(Trip driverTrip, CancellationToken cancellationToken = default);
    Task<Booking?> TryMatchRiderTripAsync(Trip riderTrip, CancellationToken cancellationToken = default);
}
