using DirectKarigar.Domain.Enums;

namespace DirectKarigar.Application.DTOs;

public record LoginRequest(string EmailOrPhone, string Password);
public record DemoLoginRequest(string Role); // customer, professional, admin

public record RegisterCustomerRequest(
    string Name,
    string Email,
    string Phone,
    string Locality,
    string Password
);

public record RegisterProfessionalRequest(
    string Name,
    string Trade,
    int ExperienceYears,
    string Email,
    string Phone,
    string Locality,
    string UpiId,
    string Password
);

public record RegisterAdminRequest(
    string Name,
    string Email,
    string Phone,
    string Password,
    string? AdminRole = null,
    string? Locality = null
);

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

public record UpdateJobStatusRequest(
    string Status
);

public record SettlePaymentRequest(
    string PaymentMode,
    string PaymentTxnId,
    decimal? Amount = null,
    bool DirectReportedByPro = false
);

public record SubmitRatingRequest(
    double Rating,
    string Comment,
    List<string>? Tags = null,
    string? Mode = "customer_rates_pro"
);

public record SubmitDisputeAppealRequest(
    string ProExplanation,
    bool HasValidProof = true
);

public record KycDocumentDto(
    string Type,
    string Title,
    string? DocumentNumberMasked,
    string FileUrl
);

public record SubmitKycRequest(
    string ProfessionalId,
    string ProfessionalName,
    string Trade,
    string Phone,
    string Locality,
    string City,
    string AadhaarNumberMasked,
    string? PanNumberMasked,
    string? SelfieUrl,
    List<KycDocumentDto>? Documents = null
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
