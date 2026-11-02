using DirectKarigar.Domain.Common;

namespace DirectKarigar.Domain.Entities;

public class ProfessionalProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    public string Trade { get; set; } = "electrician";
    public int ExperienceYears { get; set; } = 8;
    public string City { get; set; } = "Bengaluru";
    public string Locality { get; set; } = "Indiranagar";
    public double Lat { get; set; } = 12.9784;
    public double Lng { get; set; } = 77.6408;

    public bool Verified { get; set; } = true;
    public double PublicRating { get; set; } = 4.9;
    public int TotalJobsDone { get; set; } = 142;
    public int ActiveOrdersCount { get; set; } = 0;
    public int MaxConcurrentOrders { get; set; } = 2;
    public string UpiId { get; set; } = string.Empty;
    public int PenaltyPoints { get; set; } = 0;
    public int TrustScore { get; set; } = 96;

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Job> AssignedJobs { get; set; } = new List<Job>();
    public KycRecord? KycRecord { get; set; }
}

public class Review : BaseEntity
{
    public string ProfessionalProfileId { get; set; } = string.Empty;
    public ProfessionalProfile? ProfessionalProfile { get; set; }

    public string ReviewerName { get; set; } = string.Empty;
    public double Rating { get; set; } = 5.0;
    public string Comment { get; set; } = string.Empty;
}
