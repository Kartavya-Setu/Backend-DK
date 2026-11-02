using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;

namespace DirectKarigar.Application.Engines;

public record CustomerScoreResult(
    double AverageRating,
    int InternalScore,
    CustomerTier Tier,
    double DispatchPriorityMultiplier
);

public record ProMatchResult(
    ProfessionalProfile Pro,
    int MatchScore,
    string Reason
);

public static class CustomerTrustEngine
{
    /// <summary>
    /// Calculates internal customer trust score and tier.
    /// Double-Blind: As per product specifications, the customer CANNOT see their rating;
    /// it is internally processed by the matching algorithm to match them with higher-tier professionals.
    /// </summary>
    public static CustomerScoreResult CalculateScore(CustomerProfile customer)
    {
        double avg = customer.InternalProRatingCount > 0
            ? customer.InternalProRatingSum / customer.InternalProRatingCount
            : 4.5;

        double ratingFactor = (avg / 5.0) * 50; // up to 50 pts
        double paymentFactor = (customer.InternalPaymentReliability / 100.0) * 35; // up to 35 pts
        double cancellationPenalty = Math.Min(customer.InternalCancellationRate * 100, 20); // up to 20 pts penalty

        int internalScore = Math.Clamp((int)Math.Round(ratingFactor + paymentFactor - cancellationPenalty + 15), 0, 100);

        CustomerTier tier = CustomerTier.Tier2StandardVerified;
        double dispatchPriorityMultiplier = 1.0;

        if (internalScore >= 80)
        {
            tier = CustomerTier.Tier1PriorityElite;
            dispatchPriorityMultiplier = 1.5;
        }
        else if (internalScore < 50)
        {
            tier = CustomerTier.Tier3UnderReview;
            dispatchPriorityMultiplier = 0.7;
        }

        return new CustomerScoreResult(
            Math.Round(avg, 2),
            internalScore,
            tier,
            dispatchPriorityMultiplier
        );
    }

    /// <summary>
    /// Rank pros for a customer's job based on internal tier matching and capacity.
    /// </summary>
    public static List<ProMatchResult> RankProsForJob(Job job, CustomerProfile customer, IEnumerable<ProfessionalProfile> pros)
    {
        var scoreData = CalculateScore(customer);

        return pros
            .Where(p => p.ActiveOrdersCount < p.MaxConcurrentOrders)
            .Select(pro =>
            {
                double score = pro.PublicRating * 15; // up to 75
                score += Math.Min(pro.ExperienceYears * 1.5, 15);
                score -= pro.PenaltyPoints * 5;

                string reason = "Nearby active professional";
                if (scoreData.Tier == CustomerTier.Tier1PriorityElite && pro.PublicRating >= 4.8)
                {
                    score += 20;
                    reason = "Priority Match: Top-rated Karigar for High-Trust Customer";
                }
                else if (scoreData.Tier == CustomerTier.Tier3UnderReview)
                {
                    score += 5;
                    reason = "Standard verified professional";
                }

                return new ProMatchResult(
                    pro,
                    (int)Math.Round(Math.Max(10, score)),
                    reason
                );
            })
            .OrderByDescending(r => r.MatchScore)
            .ToList();
    }
}
