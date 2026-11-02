namespace DirectKarigar.Domain.Enums;

public enum ServiceCategory
{
    Electrician,
    Plumber,
    Carpenter,
    Appliance,
    Painter,
    Handyman
}

public enum JobStatus
{
    Posted,
    Negotiating,
    Assigned,
    InProgress,
    WorkCompleted,
    Paid,
    Cancelled,
    FlaggedDispute
}

public enum CustomerTier
{
    Tier1PriorityElite,
    Tier2StandardVerified,
    Tier3UnderReview
}

public enum KycVerificationStatus
{
    Pending,
    InReview,
    Verified,
    Rejected,
    ResubmitRequired
}

public enum KycDocumentType
{
    AadhaarCard,
    PanCard,
    ItiTradeCertificate,
    SkillDiploma,
    LiveSelfie,
    PoliceVerification
}

public enum AdminRole
{
    SuperAdmin,
    KycVerifier,
    DisputeOfficer,
    CityOpsManager
}

public enum UserRole
{
    Customer,
    Professional,
    Admin
}

public enum DisputeStatus
{
    FlaggedForReview,
    UnderAutoAudit,
    AppealSubmitted,
    PenaltyEnforced,
    PenaltyWaived
}

public enum DisputeFlagReason
{
    SuspectedOfflineWork,
    SilentCancelAfterVisit,
    UnreportedDirectPayment
}

public enum PaymentMode
{
    UpiInApp,
    DirectUpiSettled,
    CashSettled
}

public enum OfferStatus
{
    Pending,
    Accepted,
    Declined,
    Countered
}
