using Longa.Application.Common.Interfaces;

namespace Longa.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
