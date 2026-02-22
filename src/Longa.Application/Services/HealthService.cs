using Longa.Application.Common.Interfaces;
using Longa.Application.Common.Models;

namespace Longa.Application.Services;

public class HealthService : IHealthService
{
    private readonly IDateTime _dateTime;

    public HealthService(IDateTime dateTime)
    {
        _dateTime = dateTime;
    }

    public HealthResponse GetStatus()
    {
        return new HealthResponse("ok", _dateTime.UtcNow);
    }
}
