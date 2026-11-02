using DirectKarigar.Domain.Common;
using DirectKarigar.Domain.Enums;

namespace DirectKarigar.Domain.Entities;

public class DisputeCase : BaseEntity
{
    public string JobId { get; set; } = string.Empty;
    public Job? Job { get; set; }

    public string ProfessionalId { get; set; } = string.Empty;
    public string ProfessionalName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public DisputeFlagReason FlagReason { get; set; }
    public decimal PenaltyAmount { get; set; } = 250m;
    public DisputeStatus Status { get; set; } = DisputeStatus.FlaggedForReview;

    public bool GeoProximityMatch { get; set; }
    public bool ChatIntentFlagged { get; set; }
    public bool CustomerConfirmationReceived { get; set; }

    public string? ProExplanation { get; set; }
    public string? SystemVerdict { get; set; }
}

public class KycRecord : BaseEntity
{
    public string ProfessionalId { get; set; } = string.Empty;
    public ProfessionalProfile? Professional { get; set; }

    public string ProfessionalName { get; set; } = string.Empty;
    public string Trade { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Locality { get; set; } = string.Empty;
    public string City { get; set; } = "Bengaluru";

    // DPDP-compliant masked identifiers
    public string AadhaarNumberMasked { get; set; } = "XXXX-XXXX-9901";
    public string? PanNumberMasked { get; set; }
    public string SelfieUrl { get; set; } = string.Empty;
    public KycVerificationStatus Status { get; set; } = KycVerificationStatus.Pending;

    public string? ReviewedByAdminId { get; set; }
    public AdminRole? ReviewedByAdminRole { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public string? RejectionNotes { get; set; }

    // Risk Signals
    public double FaceMatchConfidence { get; set; } = 94.0;
    public double TamperDetectionScore { get; set; } = 98.0;
    public bool AadhaarOtpValidated { get; set; } = true;
    public string DuplicateAccountRisk { get; set; } = "low"; // low, medium, high

    public ICollection<KycDocument> Documents { get; set; } = new List<KycDocument>();
}

public class KycDocument : BaseEntity
{
    public string KycRecordId { get; set; } = string.Empty;
    public KycRecord? KycRecord { get; set; }

    public KycDocumentType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? DocumentNumberMasked { get; set; }
    public string FileUrl { get; set; } = string.Empty;
}

public class AdminAuditLog : BaseEntity
{
    public string AdminName { get; set; } = string.Empty;
    public AdminRole AdminRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty; // kyc_document, dispute_case, professional_status, locality_rule
    public string Details { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "emerald";
}

public class LocalityDemandSupply : BaseEntity
{
    public string Locality { get; set; } = string.Empty;
    public int ActivePros { get; set; }
    public int OpenJobsCount { get; set; }
    public string HealthStatus { get; set; } = "optimal"; // optimal, surplus, deficit
    public int AvgResponseTimeMinutes { get; set; } = 4;
    public double SurgeMultiplier { get; set; } = 1.0;
}
