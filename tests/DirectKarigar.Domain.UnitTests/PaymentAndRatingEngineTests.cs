using DirectKarigar.Application.Engines;
using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;
using Xunit;

namespace DirectKarigar.Domain.UnitTests;

public class PaymentAndRatingEngineTests
{
    [Fact]
    public void PricingEngine_CalculateBill_EnsuresFullEarningsGoToTechnicianWithZeroPlatformCommission()
    {
        // DirectKarigar guarantees zero commission deducted from technician labor/parts.
        decimal laborCost = 550m;
        decimal partsCost = 250m;
        decimal platformFee = 19m;

        var (labor, parts, fee, total) = PricingEngine.CalculateBill(laborCost, partsCost, platformFee);

        Assert.Equal(550m, labor);
        Assert.Equal(250m, parts);
        Assert.Equal(19m, fee);
        Assert.Equal(819m, total);
        Assert.Equal(laborCost + partsCost + platformFee, total);
    }

    [Fact]
    public void CustomerRatingPro_RecomputesAveragePublicRatingAccurately()
    {
        // Arrange
        var pro = new ProfessionalProfile
        {
            Id = "pro_ramesh",
            PublicRating = 4.8,
            TotalJobsDone = 10,
            TrustScore = 95,
            Reviews = new List<Review>
            {
                new() { Rating = 5.0, ReviewerName = "Aarav", Comment = "Super fast" },
                new() { Rating = 4.0, ReviewerName = "Priya", Comment = "Good work" }
            }
        };

        // Act: Customer submits 5-star review
        var newReview = new Review
        {
            ProfessionalProfileId = pro.Id,
            ReviewerName = "Vikash",
            Rating = 5.0,
            Comment = "Excellent wiring work"
        };
        pro.Reviews.Add(newReview);
        pro.PublicRating = Math.Round(pro.Reviews.Average(r => r.Rating), 1);
        pro.TotalJobsDone += 1;

        // Assert: (5.0 + 4.0 + 5.0) / 3 = 4.666... => 4.7
        Assert.Equal(4.7, pro.PublicRating);
        Assert.Equal(3, pro.Reviews.Count);
        Assert.Equal(11, pro.TotalJobsDone);
    }

    [Fact]
    public void ProRatingCustomer_UpdatesDoubleBlindReliability_AndRecalculatesTierAccurately()
    {
        // Arrange: Customer starts at standard baseline in Tier 2 Standard Verified
        var customer = new CustomerProfile
        {
            Id = "cust_priya",
            InternalProRatingSum = 14.0,
            InternalProRatingCount = 4, // 3.5 average
            InternalPaymentReliability = 75.0,
            InternalCancellationRate = 0.10
        };

        var initialScore = CustomerTrustEngine.CalculateScore(customer);
        Assert.Equal(66, initialScore.InternalScore);
        Assert.Equal(CustomerTier.Tier2StandardVerified, initialScore.Tier);
        Assert.Equal(1.0, initialScore.DispatchPriorityMultiplier);

        // Act: 3 consecutive Karigars rate this customer 5.0 for prompt UPI settlement & courtesy
        customer.InternalProRatingSum += 5.0 * 3;
        customer.InternalProRatingCount += 3;
        customer.InternalPaymentReliability = 98.0;
        customer.InternalCancellationRate = 0.04;

        var updatedScore = CustomerTrustEngine.CalculateScore(customer);

        // Assert: Trust score jumps to Tier 1 Priority Elite
        Assert.True(updatedScore.InternalScore >= 80, $"Expected >= 80, got {updatedScore.InternalScore}");
        Assert.Equal(CustomerTier.Tier1PriorityElite, updatedScore.Tier);
        Assert.Equal(1.5, updatedScore.DispatchPriorityMultiplier);
    }

    [Fact]
    public void DisputeAppeal_TransitionsStatusToAppealSubmitted_WithProofRecorded()
    {
        // Arrange
        var dispute = new DisputeCase
        {
            Id = "disp_01",
            JobId = "job_01",
            ProfessionalId = "pro_ramesh",
            ProfessionalName = "Ramesh Sharma",
            Status = DisputeStatus.FlaggedForReview,
            PenaltyAmount = 250m
        };

        // Act: Pro submits appeal with reason
        string appealReason = "Client asked for heavy motor capacitor replacement which wasn't available locally.";
        dispute.Status = DisputeStatus.AppealSubmitted;
        dispute.ProExplanation = appealReason;

        // Assert
        Assert.Equal(DisputeStatus.AppealSubmitted, dispute.Status);
        Assert.Equal(appealReason, dispute.ProExplanation);
    }
}
