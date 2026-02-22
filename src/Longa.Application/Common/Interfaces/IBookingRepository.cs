using Longa.Domain.Entities;

namespace Longa.Application.Common.Interfaces;

public interface IBookingRepository
{
    Task<Booking> CreateAsync(Booking booking, CancellationToken cancellationToken = default);
}
