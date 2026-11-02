using DirectKarigar.Application.Engines;
using DirectKarigar.Domain.Enums;
using Xunit;

namespace DirectKarigar.Domain.UnitTests;

public class AntiBypassAuditEngineTests
{
    [Fact]
    public void AuditCancellation_WhenProArrivedAndPhoneExchanged_FlagsSuspiciousWithPenalty()
    {
        // Arrange
        var auditParams = new CancellationAuditParams(
            ProArrivedAtLocation: true,
            PhoneExchangedInChat: true,
            CancellationTimingMinutes: 35,
            ReportedDirectPayment: false
        );

        // Act
        var result = AntiBypassAuditEngine.AuditCancellation(auditParams);

        // Assert
        Assert.True(result.IsSuspicious);
        Assert.True(result.RiskScore >= 60);
        Assert.Equal(250m, result.RecommendedPenalty);
        Assert.Equal(DisputeFlagReason.SilentCancelAfterVisit, result.FlagReason);
    }

    [Fact]
    public void AuditCancellation_WhenProHasNotArrived_DoesNotFlagSuspicious()
    {
        // Arrange
        var auditParams = new CancellationAuditParams(
            ProArrivedAtLocation: false,
            PhoneExchangedInChat: false,
            CancellationTimingMinutes: 5,
            ReportedDirectPayment: false
        );

        // Act
        var result = AntiBypassAuditEngine.AuditCancellation(auditParams);

        // Assert
        Assert.False(result.IsSuspicious);
        Assert.Equal(0m, result.RecommendedPenalty);
        Assert.Null(result.FlagReason);
    }

    [Theory]
    [InlineData(400, 150, 19, 569)]
    [InlineData(300, 0, 19, 319)]
    public void CalculateBill_AddsLaborPartsAndFlatFee(decimal labor, decimal parts, decimal fee, decimal expectedTotal)
    {
        // Act
        var (l, p, f, total) = PricingEngine.CalculateBill(labor, parts, fee);

        // Assert
        Assert.Equal(labor, l);
        Assert.Equal(parts, p);
        Assert.Equal(fee, f);
        Assert.Equal(expectedTotal, total);
    }
}
