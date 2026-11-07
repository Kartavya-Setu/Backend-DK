using DirectKarigar.Application.DTOs;
using DirectKarigar.Application.Engines;
using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;
using Xunit;

namespace DirectKarigar.Domain.UnitTests;

public class RegistrationAndDispatchEngineTests
{
    [Fact]
    public void NewlyRegisteredCustomer_InitializesWithTier1PriorityEliteScore()
    {
        // When a customer is newly registered via AuthController, baseline metrics are:
        // InternalProRatingSum = 5.0, Count = 1, CancellationRate = 0.0, PaymentReliability = 100.0
        var newCustomer = new CustomerProfile
        {
            Id = "cust_test_new",
            City = "Bengaluru",
            Locality = "Indiranagar",
            InternalProRatingSum = 5.0,
            InternalProRatingCount = 1,
            InternalCancellationRate = 0.0,
            InternalPaymentReliability = 100.0
        };

        var scoreResult = CustomerTrustEngine.CalculateScore(newCustomer);

        Assert.Equal(100, scoreResult.InternalScore);
        Assert.Equal(CustomerTier.Tier1PriorityElite, scoreResult.Tier);
        Assert.Equal(1.5, scoreResult.DispatchPriorityMultiplier);
        Assert.Equal(5.0, scoreResult.AverageRating);
    }

    [Fact]
    public void CustomerWithZeroRatingCount_FallsBackGracefullyWithoutDivisionByZero()
    {
        var zeroRatingCustomer = new CustomerProfile
        {
            Id = "cust_zero_count",
            City = "Bengaluru",
            Locality = "Koramangala",
            InternalProRatingSum = 0.0,
            InternalProRatingCount = 0,
            InternalCancellationRate = 0.0,
            InternalPaymentReliability = 100.0
        };

        var scoreResult = CustomerTrustEngine.CalculateScore(zeroRatingCustomer);

        // ratingFactor = (4.5 / 5.0) * 50 = 45; paymentFactor = 35; cancelPenalty = 0; +15 = 95
        Assert.Equal(4.5, scoreResult.AverageRating);
        Assert.True(scoreResult.InternalScore >= 80);
        Assert.Equal(CustomerTier.Tier1PriorityElite, scoreResult.Tier);
    }

    [Fact]
    public void RankProsForJob_OverloadedProsAtMaxCapacity_AreFilteredOut()
    {
        var customer = new CustomerProfile
        {
            InternalProRatingSum = 5.0,
            InternalProRatingCount = 1,
            InternalPaymentReliability = 100.0,
            InternalCancellationRate = 0.0
        };

        var availablePro = new ProfessionalProfile
        {
            Id = "pro_available",
            PublicRating = 4.7,
            ExperienceYears = 5,
            ActiveOrdersCount = 1,
            MaxConcurrentOrders = 3 // Not full
        };

        var fullCapacityPro = new ProfessionalProfile
        {
            Id = "pro_busy",
            PublicRating = 5.0,
            ExperienceYears = 10,
            ActiveOrdersCount = 3,
            MaxConcurrentOrders = 3 // Saturated
        };

        var job = new Job();

        var ranked = CustomerTrustEngine.RankProsForJob(job, customer, new[] { availablePro, fullCapacityPro });

        Assert.Single(ranked);
        Assert.Equal("pro_available", ranked.First().Pro.Id);
    }

    [Fact]
    public void RegisterCustomerRequest_DtoContract_InstantiatesCorrectly()
    {
        var req = new RegisterCustomerRequest(
            Name: "Test Customer",
            Email: "customer@karigardirect.in",
            Phone: "+91 99999 11111",
            Locality: "Indiranagar",
            Password: "SecurePassword123!"
        );

        Assert.Equal("Test Customer", req.Name);
        Assert.Equal("customer@karigardirect.in", req.Email);
        Assert.Equal("+91 99999 11111", req.Phone);
        Assert.Equal("Indiranagar", req.Locality);
    }

    [Fact]
    public void RegisterProfessionalRequest_DtoContract_InstantiatesCorrectly()
    {
        var req = new RegisterProfessionalRequest(
            Name: "Ramesh Sharma",
            Trade: "Plumber",
            ExperienceYears: 8,
            Email: "ramesh@karigardirect.in",
            Phone: "+91 98888 22222",
            Locality: "Koramangala",
            UpiId: "ramesh@upi",
            Password: "SecurePassword123!"
        );

        Assert.Equal("Ramesh Sharma", req.Name);
        Assert.Equal("Plumber", req.Trade);
        Assert.Equal(8, req.ExperienceYears);
        Assert.Equal("ramesh@upi", req.UpiId);
    }
}
