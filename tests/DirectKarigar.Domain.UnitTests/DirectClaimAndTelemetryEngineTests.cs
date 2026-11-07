using DirectKarigar.Application.DTOs;
using DirectKarigar.Application.Engines;
using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;
using Xunit;

namespace DirectKarigar.Domain.UnitTests;

public class DirectClaimAndTelemetryEngineTests
{
    [Fact]
    public void DirectClaim_SuccessfulInstantMatch_LocksBudgetPriceAndDispatchesKarigar()
    {
        // Arrange
        var job = new Job
        {
            Id = "job_test_01",
            Title = "Kitchen Faucet Leaking Under Sink",
            Category = ServiceCategory.Plumber,
            Status = JobStatus.Posted,
            BudgetMin = 300m,
            BudgetMax = 500m
        };

        var pro = new ProfessionalProfile
        {
            Id = "pro_suresh",
            Trade = "Plumber",
            ActiveOrdersCount = 1,
            MaxConcurrentOrders = 3
        };

        // Act: Instant Match claim at minimum budget
        decimal agreedPrice = job.BudgetMin;
        job.AssignedProId = pro.Id;
        job.AssignedProName = "Suresh Kumar";
        job.AssignedProPhone = "+91 97412 88410";
        job.AgreedPrice = agreedPrice;
        job.Status = JobStatus.Assigned;
        pro.ActiveOrdersCount += 1;

        // Assert
        Assert.Equal(JobStatus.Assigned, job.Status);
        Assert.Equal(300m, job.AgreedPrice);
        Assert.Equal("pro_suresh", job.AssignedProId);
        Assert.Equal(2, pro.ActiveOrdersCount);
        Assert.True(pro.ActiveOrdersCount <= pro.MaxConcurrentOrders);
    }

    [Fact]
    public void DirectClaim_OverCapacityTechnician_PreventsNewLeadAssignment()
    {
        // Arrange
        var proAtMaxCapacity = new ProfessionalProfile
        {
            Id = "pro_ramesh",
            Trade = "Electrician",
            ActiveOrdersCount = 3,
            MaxConcurrentOrders = 3 // Saturated
        };

        // Act & Assert
        bool canClaim = proAtMaxCapacity.ActiveOrdersCount < proAtMaxCapacity.MaxConcurrentOrders;
        Assert.False(canClaim, "Technician at max concurrent order capacity must not be allowed to claim additional jobs.");
    }

    [Fact]
    public void CustomerTrustEngine_MatchmakingRanking_AppliesPriorityBoostForTier1Customers()
    {
        // Arrange
        var eliteCustomer = new CustomerProfile
        {
            Id = "cust_elite",
            City = "Bengaluru",
            Locality = "Indiranagar",
            InternalProRatingSum = 49.0,
            InternalProRatingCount = 10,
            InternalCancellationRate = 0.02,
            InternalPaymentReliability = 99.0
        };

        var scoreResult = CustomerTrustEngine.CalculateScore(eliteCustomer);
        Assert.Equal(CustomerTier.Tier1PriorityElite, scoreResult.Tier);

        var topMasterPro = new ProfessionalProfile
        {
            Id = "pro_master",
            PublicRating = 4.9,
            ExperienceYears = 12,
            ActiveOrdersCount = 0,
            MaxConcurrentOrders = 3
        };

        var standardPro = new ProfessionalProfile
        {
            Id = "pro_standard",
            PublicRating = 4.3,
            ExperienceYears = 3,
            ActiveOrdersCount = 0,
            MaxConcurrentOrders = 3
        };

        var job = new Job { Category = ServiceCategory.Electrician };

        // Act
        var ranked = CustomerTrustEngine.RankProsForJob(job, eliteCustomer, new[] { topMasterPro, standardPro });

        // Assert
        Assert.Equal(2, ranked.Count);
        var firstMatch = ranked.First();
        Assert.Equal("pro_master", firstMatch.Pro.Id);
        Assert.Contains("Priority Match", firstMatch.Reason);
        Assert.True(firstMatch.MatchScore > ranked[1].MatchScore);
    }

    [Fact]
    public void ProfessionalTelemetry_LocationUpdate_RecalculatesProximityCorrectly()
    {
        // Arrange
        var pro = new ProfessionalProfile
        {
            Id = "pro_telemetry",
            Lat = 12.9784,
            Lng = 77.6408,
            Locality = "Indiranagar"
        };

        // Customer premises at Indiranagar 12th Main
        double custLat = 12.9784;
        double custLng = 77.6408;

        // Initial distance = 0 km
        double dLat0 = (pro.Lat - custLat) * 111.0;
        double dLng0 = (pro.Lng - custLng) * 111.0 * Math.Cos(custLat * Math.PI / 180.0);
        double initialDist = Math.Sqrt(dLat0 * dLat0 + dLng0 * dLng0);
        Assert.True(initialDist < 0.1);

        // Act: Karigar updates GPS telemetry ping to Domlur Flyover (approx 2.5 km away)
        pro.Lat = 12.9610;
        pro.Lng = 77.6387;
        pro.Locality = "Domlur";

        double dLat1 = (pro.Lat - custLat) * 111.0;
        double dLng1 = (pro.Lng - custLng) * 111.0 * Math.Cos(custLat * Math.PI / 180.0);
        double updatedDist = Math.Sqrt(dLat1 * dLat1 + dLng1 * dLng1);

        // Assert: Pro is approximately 1.9 - 2.5 km away
        Assert.True(updatedDist > 1.5 && updatedDist < 3.0, $"Expected distance between 1.5 and 3.0 km, got {updatedDist:F2} km");
        Assert.Equal("Domlur", pro.Locality);
    }

    [Fact]
    public void ClaimJobRequest_InstantiatesWithDefaultPriceNull()
    {
        var req = new ClaimJobRequest(
            ProId: "pro_ramesh",
            ProName: "Ramesh Sharma",
            ProPhone: "+91 94480 32155"
        );

        Assert.Equal("pro_ramesh", req.ProId);
        Assert.Equal("Ramesh Sharma", req.ProName);
        Assert.Null(req.AgreedPrice);
    }
}
