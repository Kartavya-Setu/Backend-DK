using DirectKarigar.Domain.Common;
using DirectKarigar.Domain.Enums;

namespace DirectKarigar.Domain.Entities;

public class Job : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public ServiceCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = "Bengaluru";
    public string Locality { get; set; } = string.Empty;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public double? DistanceKm { get; set; }

    public decimal BudgetMin { get; set; }
    public decimal BudgetMax { get; set; }
    public decimal? AgreedPrice { get; set; }
    public string Urgency { get; set; } = "immediate"; // immediate, today, scheduled
    public string? ScheduledDate { get; set; }
    public string? PreferredTimeSlot { get; set; }
    public List<string> Photos { get; set; } = new();

    public JobStatus Status { get; set; } = JobStatus.Posted;

    public string CustomerId { get; set; } = string.Empty;
    public CustomerProfile? Customer { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerLocality { get; set; } = string.Empty;

    public string? AssignedProId { get; set; }
    public ProfessionalProfile? AssignedPro { get; set; }
    public string? AssignedProName { get; set; }
    public string? AssignedProPhone { get; set; }

    public WorkBill? Bill { get; set; }
    public PaymentMode? PaymentMode { get; set; }
    public string? PaymentTxnId { get; set; }
    public bool DirectPaymentReportedByPro { get; set; }
    public bool CustomerReviewSubmitted { get; set; }
    public bool ProReviewSubmitted { get; set; }
    public string? DisputeId { get; set; }

    // Concurrency Token for optimistic locking on state updates
    public byte[]? RowVersion { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage : BaseEntity
{
    public string JobId { get; set; } = string.Empty;
    public Job? Job { get; set; }

    public string SenderId { get; set; } = string.Empty;
    public string SenderRole { get; set; } = "customer"; // customer, professional, system
    public string SenderName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public bool IsOffer { get; set; }
    public decimal? OfferAmount { get; set; }
    public OfferStatus? OfferStatus { get; set; }
}

public class WorkBill : BaseEntity
{
    public string JobId { get; set; } = string.Empty;
    public Job? Job { get; set; }

    public decimal LaborCost { get; set; }
    public decimal PartsCost { get; set; }
    public string? PartsDescription { get; set; }
    public decimal PlatformConvenienceFee { get; set; } = 19m; // DirectKarigar flat fee
    public decimal TotalAmount { get; set; }
    public string? ProNotes { get; set; }
}
