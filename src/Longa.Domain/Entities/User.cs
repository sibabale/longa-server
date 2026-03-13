namespace Longa.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string? DeviceMake { get; set; }
    public string? DeviceModel { get; set; }
    public string? IdentifierForVendor { get; set; }

    // Required fields
    public required string Email {get; set;}
    public required string FullName {get; set;}
    public required string Auth0UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public PushToken? PushToken { get; set; }
}
