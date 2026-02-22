namespace Longa.Application.Common.Models;

public record CreateTripRequest(
    string Role,
    string PickupAddress,
    decimal PickupLat,
    decimal PickupLng,
    string DestinationAddress,
    decimal DestinationLat,
    decimal DestinationLng,
    DateTimeOffset DepartureAt,
    string? PriceDisplay
);

public record TripResponse(
    Guid Id,
    Guid UserId,
    string Role,
    string Status,
    string PickupAddress,
    decimal PickupLat,
    decimal PickupLng,
    string DestinationAddress,
    decimal DestinationLat,
    decimal DestinationLng,
    DateTimeOffset DepartureAt,
    string? PriceDisplay,
    DateTimeOffset CreatedAt
);

public record CreateUserRequest(
    string IdentifierForVendor,
    string? DeviceModel,
    string? DeviceMake
);

public record UserResponse(
    Guid Id,
    string IdentifierForVendor,
    string? DeviceModel,
    string? DeviceMake,
    DateTimeOffset CreatedAt
);

public record PutPushTokenRequest(string Token);

public record CreateBookingRequest(
    Guid DriverTripId,
    string PickupAddress,
    decimal PickupLat,
    decimal PickupLng,
    string DestinationAddress,
    decimal DestinationLat,
    decimal DestinationLng,
    DateTimeOffset DepartureAt
);

public record BookingResponse(
    Guid Id,
    Guid DriverTripId,
    Guid RiderTripId,
    DateTimeOffset CreatedAt
);

public record CreateBookingDriverSelectRequest(
    Guid RiderTripId,
    string PickupAddress,
    decimal PickupLat,
    decimal PickupLng,
    string DestinationAddress,
    decimal DestinationLat,
    decimal DestinationLng,
    DateTimeOffset DepartureAt,
    string PriceDisplay
);
