using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;

namespace DirectKarigar.Application.Engines;

public record CancellationAuditParams(
    bool ProArrivedAtLocation,
    bool PhoneExchangedInChat,
    int CancellationTimingMinutes,
    bool ReportedDirectPayment
);

public record CancellationAuditResult(
    int RiskScore,
    bool IsSuspicious,
    decimal RecommendedPenalty,
    DisputeFlagReason? FlagReason
);

public static class AntiBypassAuditEngine
{
    /// <summary>
    /// Evaluates whether an order cancellation might be an offline bypass.
    /// (e.g. silent cancel on app but take care of work offline to avoid platform fee/records)
    /// </summary>
    public static CancellationAuditResult AuditCancellation(CancellationAuditParams param)
    {
        int riskScore = 0;

        if (param.ProArrivedAtLocation)
            riskScore += 45; // High suspicion if cancelled after reaching customer location

        if (param.PhoneExchangedInChat)
            riskScore += 25; // Contact details exchanged before cancelling

        if (param.CancellationTimingMinutes > 30)
            riskScore += 20; // Order held for substantial time then dropped

        if (!param.ReportedDirectPayment && param.ProArrivedAtLocation)
            riskScore += 15;

        bool isSuspicious = riskScore >= 60;
        decimal recommendedPenalty = isSuspicious ? 250m : 0m;

        DisputeFlagReason? flagReason = null;
        if (isSuspicious)
        {
            flagReason = param.ProArrivedAtLocation
                ? DisputeFlagReason.SilentCancelAfterVisit
                : DisputeFlagReason.SuspectedOfflineWork;
        }

        return new CancellationAuditResult(riskScore, isSuspicious, recommendedPenalty, flagReason);
    }
}

public static class PricingEngine
{
    /// <summary>
    /// Calculates transparent pricing with 0% middleman commission on labor.
    /// Flat ₹19 platform convenience fee.
    /// </summary>
    public static (decimal labor, decimal parts, decimal fee, decimal total) CalculateBill(
        decimal laborCost,
        decimal partsCost,
        decimal platformFee = 19m)
    {
        var labor = Math.Max(0m, laborCost);
        var parts = Math.Max(0m, partsCost);
        var fee = Math.Max(0m, platformFee);
        var total = labor + parts + fee;

        return (labor, parts, fee, total);
    }
}
