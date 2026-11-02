using DirectKarigar.Application.Engines;
using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;
using Xunit;

namespace DirectKarigar.Domain.UnitTests;

public class CustomerTrustEngineTests
{
    [Fact]
    public void CalculateScore_HighRatingAndPayment_ReturnsTier1PriorityElite()
    {
        // Arrange
        var customer = new CustomerProfile
        {
            InternalProRatingSum = 49.0,
            InternalProRatingCount = 10, // 4.9 average
            InternalPaymentReliability = 98.0,
            InternalCancellationRate = 0.02
        };

        // Act
        var result = CustomerTrustEngine.CalculateScore(customer);

        // Assert
        Assert.True(result.InternalScore >= 80, $"Expected score >= 80, got {result.InternalScore}");
        Assert.Equal(CustomerTier.Tier1PriorityElite, result.Tier);
        Assert.Equal(1.5, result.DispatchPriorityMultiplier);
    }

    [Fact]
    public void CalculateScore_HighCancellationAndLowRating_ReturnsTier3UnderReview()
    {
        // Arrange
        var customer = new CustomerProfile
        {
            InternalProRatingSum = 4.0,
            InternalProRatingCount = 2, // 2.0 average
            InternalPaymentReliability = 40.0,
            InternalCancellationRate = 0.40 // 40% cancel rate
        };

        // Act
        var result = CustomerTrustEngine.CalculateScore(customer);

        // Assert
        Assert.True(result.InternalScore < 50, $"Expected score < 50, got {result.InternalScore}");
        Assert.Equal(CustomerTier.Tier3UnderReview, result.Tier);
        Assert.Equal(0.7, result.DispatchPriorityMultiplier);
    }

    [Fact]
    public void RankProsForJob_Tier1Customer_PrioritizesTopRatedProsWithBonus()
    {
        // Arrange
        var customer = new CustomerProfile
        {
            InternalProRatingSum = 49.0,
            InternalProRatingCount = 10,
            InternalPaymentReliability = 99.0,
            InternalCancellationRate = 0.01
        };

        var masterPro = new ProfessionalProfile
        {
            Id = "pro_master",
            PublicRating = 4.9,
            ExperienceYears = 12,
            ActiveOrdersCount = 0,
            MaxConcurrentOrders = 3
        };

        var averagePro = new ProfessionalProfile
        {
            Id = "pro_avg",
            PublicRating = 4.2,
            ExperienceYears = 3,
            ActiveOrdersCount = 0,
            MaxConcurrentOrders = 3
        };

        var job = new Job();

        // Act
        var ranked = CustomerTrustEngine.RankProsForJob(job, customer, new[] { averagePro, masterPro });

        // Assert
        Assert.Equal("pro_master", ranked.First().Pro.Id);
        Assert.Contains("Priority Match", ranked.First().Reason);
    }
}
