namespace Longa.Domain.Entities;

public class IdempotencyKey
{
    public Guid Key { get; set; }
    public Guid UserId { get; set; }
    public Guid TripId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
