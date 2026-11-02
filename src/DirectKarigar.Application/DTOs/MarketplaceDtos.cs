using DirectKarigar.Domain.Enums;

namespace DirectKarigar.Application.DTOs;

public record LoginRequest(string EmailOrPhone, string Password);
public record DemoLoginRequest(string Role); // customer, professional, admin

public record AuthUserDto(
    string Id,
    string Name,
    string Email,
    string Phone,
    string Role,
    string? AdminRole,
    string? ProId,
    string? CustomerId,
    string AvatarBg,
    string Initials,
    string? Locality,
    string? City,
    string Token
);

public record CreateJobRequest(
    string Title,
    string Category,
    string Description,
    string City,
    string Locality,
    double? Lat,
    double? Lng,
    decimal BudgetMin,
    decimal BudgetMax,
    string Urgency,
    string? ScheduledDate,
    string? PreferredTimeSlot,
    List<string>? Photos
);

public record SendMessageRequest(
    string Text,
    bool IsOffer = false,
    decimal? OfferAmount = null
);

public record CreateBillRequest(
    decimal LaborCost,
    decimal PartsCost,
    string? PartsDescription,
    decimal PlatformConvenienceFee = 19m,
    string? ProNotes = null
);

public record SubmitRatingRequest(
    double Rating,
    string Comment,
    List<string>? Tags = null
);

public record ResolveDisputeRequest(
    string Verdict,
    bool WaivePenalty
);

public record ReviewKycRequest(
    string? Reason,
    string? Notes
);

public record UpdateSurgeRequest(
    double Multiplier
);
