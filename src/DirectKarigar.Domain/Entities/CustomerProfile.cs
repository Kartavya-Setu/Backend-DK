using DirectKarigar.Domain.Common;

namespace DirectKarigar.Domain.Entities;

public class CustomerProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    public string City { get; set; } = "Bengaluru";
    public string Locality { get; set; } = "Indiranagar";
    public double Lat { get; set; } = 12.9716;
    public double Lng { get; set; } = 77.5946;

    // Double-blind internal rating metrics (NOT visible to customer)
    public double InternalProRatingSum { get; set; } = 24.5;
    public int InternalProRatingCount { get; set; } = 5;
    public double InternalCancellationRate { get; set; } = 0.05;
    public double InternalPaymentReliability { get; set; } = 98.0;

    public ICollection<Job> PostedJobs { get; set; } = new List<Job>();
}
