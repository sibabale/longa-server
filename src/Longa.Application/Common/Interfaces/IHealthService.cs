using Longa.Application.Common.Models;

namespace Longa.Application.Common.Interfaces;

public interface IHealthService
{
    HealthResponse GetStatus();
}
