using DirectKarigar.Application.DTOs;
using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;
using DirectKarigar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DirectKarigar.WebApi.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminOpsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminOpsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("kyc")]
    public async Task<ActionResult<List<KycRecord>>> GetKycRecords([FromQuery] string? status)
    {
        var query = _context.KycRecords.Include(k => k.Documents).AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<KycVerificationStatus>(status, true, out var s))
        {
            query = query.Where(k => k.Status == s);
        }

        return Ok(await query.ToListAsync());
    }

    [HttpPost("kyc/{id}/approve")]
    public async Task<ActionResult> ApproveKyc(string id)
    {
        var record = await _context.KycRecords.FirstOrDefaultAsync(k => k.Id == id);
        if (record == null) return NotFound();

        record.Status = KycVerificationStatus.Verified;
        record.ReviewedAtUtc = DateTime.UtcNow;
        record.ReviewedByAdminId = "user_admin_vikram";
        record.ReviewedByAdminRole = AdminRole.KycVerifier;

        var pro = await _context.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == record.ProfessionalId);
        if (pro != null)
        {
            pro.Verified = true;
            pro.TrustScore = Math.Min(100, pro.TrustScore + 10);
        }

        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminName = "Vikram Malhotra",
            AdminRole = AdminRole.KycVerifier,
            Action = "APPROVED_KYC_CREDENTIALS",
            TargetId = record.ProfessionalId,
            TargetType = "kyc_document",
            Details = $"Approved UIDAI Aadhaar & trade license for {record.ProfessionalName} ({record.Trade}). Issued Verified Karigar badge.",
            BadgeColor = "emerald"
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "KYC verified successfully." });
    }

    [HttpPost("kyc/{id}/reject")]
    public async Task<ActionResult> RejectKyc(string id, [FromBody] ReviewKycRequest request)
    {
        var record = await _context.KycRecords.FirstOrDefaultAsync(k => k.Id == id);
        if (record == null) return NotFound();

        record.Status = KycVerificationStatus.Rejected;
        record.RejectionReason = request.Reason ?? "Document unreadable or invalid";
        record.RejectionNotes = request.Notes;
        record.ReviewedAtUtc = DateTime.UtcNow;

        var pro = await _context.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == record.ProfessionalId);
        if (pro != null)
        {
            pro.Verified = false;
            pro.TrustScore = Math.Max(20, pro.TrustScore - 30);
        }

        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminName = "Vikram Malhotra",
            AdminRole = AdminRole.KycVerifier,
            Action = "REJECTED_KYC_CREDENTIALS",
            TargetId = record.ProfessionalId,
            TargetType = "kyc_document",
            Details = $"Rejected KYC for {record.ProfessionalName}: \"{request.Reason}\".",
            BadgeColor = "rose"
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "KYC rejected." });
    }

    [HttpGet("disputes")]
    public async Task<ActionResult<List<DisputeCase>>> GetDisputes()
    {
        return Ok(await _context.DisputeCases.AsNoTracking().ToListAsync());
    }

    [HttpPost("disputes/{id}/resolve")]
    public async Task<ActionResult> ResolveDispute(string id, [FromBody] ResolveDisputeRequest request)
    {
        var dispute = await _context.DisputeCases.FirstOrDefaultAsync(d => d.Id == id);
        if (dispute == null) return NotFound();

        dispute.Status = request.WaivePenalty ? DisputeStatus.PenaltyWaived : DisputeStatus.PenaltyEnforced;
        dispute.SystemVerdict = request.Verdict;

        var pro = await _context.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == dispute.ProfessionalId);
        if (pro != null)
        {
            if (request.WaivePenalty)
            {
                pro.PenaltyPoints = Math.Max(0, pro.PenaltyPoints - 1);
            }
            else
            {
                pro.PenaltyPoints += 1;
            }
        }

        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminName = "Vikram Malhotra",
            AdminRole = AdminRole.DisputeOfficer,
            Action = request.WaivePenalty ? "WAIVED_DISPUTE_PENALTY" : "ENFORCED_DISPUTE_PENALTY",
            TargetId = id,
            TargetType = "dispute_case",
            Details = $"Dispute #{id}: Applied verdict \"{request.Verdict}\". Penalty {(request.WaivePenalty ? "waived" : "enforced")}.",
            BadgeColor = request.WaivePenalty ? "emerald" : "rose"
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Dispute resolved successfully." });
    }

    [HttpGet("locality-metrics")]
    public async Task<ActionResult<List<LocalityDemandSupply>>> GetLocalityMetrics()
    {
        return Ok(await _context.LocalityDemandSupplies.AsNoTracking().ToListAsync());
    }

    [HttpPatch("locality-metrics/{locality}/surge")]
    public async Task<ActionResult> UpdateSurge(string locality, [FromBody] UpdateSurgeRequest request)
    {
        var metric = await _context.LocalityDemandSupplies.FirstOrDefaultAsync(l => l.Locality.ToLower() == locality.ToLower());
        if (metric == null) return NotFound();

        metric.SurgeMultiplier = request.Multiplier;

        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminName = "Vikram Malhotra",
            AdminRole = AdminRole.CityOpsManager,
            Action = "UPDATED_SURGE_MULTIPLIER",
            TargetId = locality,
            TargetType = "locality_rule",
            Details = $"Surge rate adjusted to {request.Multiplier}x for {locality}.",
            BadgeColor = "blue"
        });

        await _context.SaveChangesAsync();
        return Ok(metric);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<List<AdminAuditLog>>> GetAuditLogs()
    {
        return Ok(await _context.AdminAuditLogs.OrderByDescending(a => a.CreatedAtUtc).ToListAsync());
    }
}
