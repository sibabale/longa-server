using Longa.Application.Common.Interfaces;
using Longa.Domain.Entities;
using Longa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Longa.Infrastructure.Repositories;

public class BookingRepository(LongaDbContext db) : IBookingRepository
{
    public async Task<Booking> CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);
        return booking;
    }
}
