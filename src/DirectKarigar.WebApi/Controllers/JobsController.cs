using DirectKarigar.Application.DTOs;
using DirectKarigar.Application.Engines;
using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;
using DirectKarigar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DirectKarigar.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class JobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public JobsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Job>>> GetJobs([FromQuery] string? status, [FromQuery] string? customerId, [FromQuery] string? proId)
    {
        var query = _context.Jobs
            .Include(j => j.Messages)
            .Include(j => j.Bill)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(j => j.CustomerId == customerId);

        if (!string.IsNullOrEmpty(proId))
            query = query.Where(j => j.AssignedProId == proId || j.Status == JobStatus.Posted);

        var list = await query.OrderByDescending(j => j.CreatedAtUtc).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Job>> GetJob(string id)
    {
        var job = await _context.Jobs
            .Include(j => j.Messages)
            .Include(j => j.Bill)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound(new { message = $"Job {id} not found" });

        return Ok(job);
    }

    [HttpPost]
    public async Task<ActionResult<Job>> PostJob([FromBody] CreateJobRequest request)
    {
        // Parse category enum safely
        if (!Enum.TryParse<ServiceCategory>(request.Category, true, out var category))
            category = ServiceCategory.Electrician;

        var job = new Job
        {
            Id = $"job_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            Title = request.Title,
            Category = category,
            Description = request.Description,
            City = request.City,
            Locality = request.Locality,
            Lat = request.Lat ?? 12.9716,
            Lng = request.Lng ?? 77.5946,
            BudgetMin = request.BudgetMin,
            BudgetMax = request.BudgetMax,
            Urgency = request.Urgency,
            ScheduledDate = request.ScheduledDate,
            PreferredTimeSlot = request.PreferredTimeSlot,
            Photos = request.Photos ?? new(),
            Status = JobStatus.Posted,
            CustomerId = "cust_aarav",
            CustomerName = "Aarav Patel",
            CustomerLocality = request.Locality,
            Messages = new List<ChatMessage>
            {
                new()
                {
                    SenderId = "system",
                    SenderRole = "system",
                    SenderName = "KarigarDirect Radar",
                    Text = $"Requirement broadcasted to verified local {category}s within 3km of {request.Locality}. Zero middleman commission applied."
                }
            }
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }

    [HttpPost("{id}/messages")]
    public async Task<ActionResult<ChatMessage>> SendMessage(string id, [FromBody] SendMessageRequest request)
    {
        var job = await _context.Jobs.Include(j => j.Messages).FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();

        var msg = new ChatMessage
        {
            JobId = id,
            SenderId = "cust_aarav",
            SenderRole = "customer",
            SenderName = "Aarav Patel",
            Text = request.Text,
            IsOffer = request.IsOffer,
            OfferAmount = request.OfferAmount,
            OfferStatus = request.IsOffer ? OfferStatus.Pending : null
        };

        job.Messages.Add(msg);
        if (request.IsOffer && job.Status == JobStatus.Posted)
        {
            job.Status = JobStatus.Negotiating;
        }

        await _context.SaveChangesAsync();
        return Ok(msg);
    }

    [HttpPost("{id}/offers/{msgId}/accept")]
    public async Task<ActionResult<Job>> AcceptOffer(string id, string msgId)
    {
        var job = await _context.Jobs.Include(j => j.Messages).FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();

        var offerMsg = job.Messages.FirstOrDefault(m => m.Id == msgId && m.IsOffer);
        if (offerMsg == null) return BadRequest(new { message = "Offer not found on this job." });

        offerMsg.OfferStatus = OfferStatus.Accepted;
        job.AgreedPrice = offerMsg.OfferAmount;
        job.Status = JobStatus.Assigned;

        job.Messages.Add(new ChatMessage
        {
            JobId = id,
            SenderId = "system",
            SenderRole = "system",
            SenderName = "KarigarDirect System",
            Text = $"Offer of ₹{offerMsg.OfferAmount} accepted! Rate locked. Karigar is dispatched. 100% of the funds go directly to the technician."
        });

        await _context.SaveChangesAsync();
        return Ok(job);
    }

    [HttpPost("{id}/bill")]
    public async Task<ActionResult<Job>> CreateBill(string id, [FromBody] CreateBillRequest request)
    {
        var job = await _context.Jobs.Include(j => j.Bill).FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();

        var (labor, parts, fee, total) = PricingEngine.CalculateBill(request.LaborCost, request.PartsCost, request.PlatformConvenienceFee);

        var bill = new WorkBill
        {
            JobId = id,
            LaborCost = labor,
            PartsCost = parts,
            PartsDescription = request.PartsDescription,
            PlatformConvenienceFee = fee,
            TotalAmount = total,
            ProNotes = request.ProNotes
        };

        job.Bill = bill;
        job.Status = JobStatus.WorkCompleted;

        await _context.SaveChangesAsync();
        return Ok(job);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<Job>> UpdateStatus(string id, [FromBody] UpdateJobStatusRequest request)
    {
        var job = await _context.Jobs.Include(j => j.Messages).FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();

        if (Enum.TryParse<JobStatus>(request.Status, true, out var status))
        {
            job.Status = status;
            await _context.SaveChangesAsync();
        }

        return Ok(job);
    }

    [HttpPost("{id}/settle")]
    public async Task<ActionResult<Job>> SettleJob(string id, [FromBody] SettlePaymentRequest request)
    {
        var job = await _context.Jobs
            .Include(j => j.Messages)
            .Include(j => j.Bill)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound(new { message = $"Job {id} not found" });

        var mode = PaymentMode.UpiInApp;
        var modeStr = request.PaymentMode?.ToLowerInvariant() ?? "";
        if (modeStr.Contains("direct") || modeStr.Contains("upi_settled"))
        {
            mode = PaymentMode.DirectUpiSettled;
        }
        else if (modeStr.Contains("cash"))
        {
            mode = PaymentMode.CashSettled;
        }

        job.Status = JobStatus.Paid;
        job.PaymentMode = mode;
        job.PaymentTxnId = request.PaymentTxnId;
        job.DirectPaymentReportedByPro = request.DirectReportedByPro;

        decimal amount = request.Amount ?? (job.Bill?.TotalAmount ?? job.AgreedPrice ?? 350m);

        // Update professional statistics if assigned
        if (!string.IsNullOrEmpty(job.AssignedProId))
        {
            var pro = await _context.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == job.AssignedProId);
            if (pro != null)
            {
                pro.TotalJobsDone += 1;
                pro.ActiveOrdersCount = Math.Max(0, pro.ActiveOrdersCount - 1);
            }
        }

        // Update customer payment reliability
        if (!string.IsNullOrEmpty(job.CustomerId))
        {
            var customer = await _context.CustomerProfiles.FirstOrDefaultAsync(c => c.Id == job.CustomerId);
            if (customer != null)
            {
                customer.InternalPaymentReliability = Math.Min(100.0, customer.InternalPaymentReliability + 0.5);
            }
        }

        job.Messages.Add(new ChatMessage
        {
            JobId = id,
            SenderId = "system",
            SenderRole = "system",
            SenderName = "DirectKarigar Settlement Guard",
            Text = $"Payment of ₹{amount} settled directly to {(request.DirectReportedByPro ? (job.AssignedProName ?? "Karigar") : "the technician")} via {mode} (Ref: {request.PaymentTxnId}). Zero platform commission deducted (100% Karigar Direct)."
        });

        await _context.SaveChangesAsync();
        return Ok(job);
    }

    [HttpPost("{id}/ratings")]
    public async Task<ActionResult<Job>> SubmitRating(string id, [FromBody] SubmitRatingRequest request)
    {
        var job = await _context.Jobs
            .Include(j => j.Messages)
            .Include(j => j.Bill)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound(new { message = $"Job {id} not found" });

        bool isCustomerRatingPro = string.Equals(request.Mode, "customer_rates_pro", StringComparison.OrdinalIgnoreCase)
                                   || string.IsNullOrEmpty(request.Mode);

        if (isCustomerRatingPro)
        {
            job.CustomerReviewSubmitted = true;
            if (!string.IsNullOrEmpty(job.AssignedProId))
            {
                var pro = await _context.ProfessionalProfiles
                    .Include(p => p.Reviews)
                    .FirstOrDefaultAsync(p => p.Id == job.AssignedProId);

                if (pro != null)
                {
                    var review = new Review
                    {
                        ProfessionalProfileId = pro.Id,
                        ReviewerName = job.CustomerName,
                        Rating = request.Rating,
                        Comment = string.IsNullOrWhiteSpace(request.Comment) ? "Verified direct service completion" : request.Comment
                    };

                    pro.Reviews.Add(review);
                    pro.PublicRating = Math.Round(pro.Reviews.Average(r => r.Rating), 1);
                    pro.TrustScore = Math.Min(100, pro.TrustScore + 1);
                }
            }

            job.Messages.Add(new ChatMessage
            {
                JobId = id,
                SenderId = "system",
                SenderRole = "system",
                SenderName = "DirectKarigar Trust Engine",
                Text = $"Customer submitted public rating ({request.Rating:F1}★). Karigar reputation verified and updated."
            });
        }
        else
        {
            // Pro rates customer: double-blind internal reliability feedback
            job.ProReviewSubmitted = true;
            if (!string.IsNullOrEmpty(job.CustomerId))
            {
                var customer = await _context.CustomerProfiles.FirstOrDefaultAsync(c => c.Id == job.CustomerId);
                if (customer != null)
                {
                    customer.InternalProRatingSum += request.Rating;
                    customer.InternalProRatingCount += 1;
                }
            }

            job.Messages.Add(new ChatMessage
            {
                JobId = id,
                SenderId = "system",
                SenderRole = "system",
                SenderName = "DirectKarigar Trust Engine",
                Text = $"Karigar submitted double-blind reliability rating ({request.Rating:F1}★). Internal priority matchmaking score updated."
            });
        }

        await _context.SaveChangesAsync();
        return Ok(job);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelJob(string id)
    {
        var job = await _context.Jobs.Include(j => j.Messages).FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();

        bool proArrived = job.Status == JobStatus.InProgress;
        var audit = AntiBypassAuditEngine.AuditCancellation(new CancellationAuditParams(
            ProArrivedAtLocation: proArrived,
            PhoneExchangedInChat: job.Messages.Count > 2,
            CancellationTimingMinutes: 35,
            ReportedDirectPayment: false
        ));

        if (audit.IsSuspicious)
        {
            var dispute = new DisputeCase
            {
                JobId = job.Id,
                ProfessionalId = job.AssignedProId ?? "pro_ramesh",
                ProfessionalName = job.AssignedProName ?? "Ramesh Sharma",
                CustomerName = $"{job.CustomerName} ({job.Locality})",
                FlagReason = audit.FlagReason ?? DisputeFlagReason.SilentCancelAfterVisit,
                PenaltyAmount = audit.RecommendedPenalty,
                Status = DisputeStatus.FlaggedForReview,
                GeoProximityMatch = proArrived,
                ChatIntentFlagged = true,
                CustomerConfirmationReceived = false,
                SystemVerdict = "Flagged by Anti-Bypass Guard: Cancellation occurred while pro was active on site. ₹250 penalty pending appeal proof."
            };

            _context.DisputeCases.Add(dispute);
            job.Status = JobStatus.FlaggedDispute;
        }
        else
        {
            job.Status = JobStatus.Cancelled;
        }

        await _context.SaveChangesAsync();
        return Ok(new { status = job.Status.ToString(), audit });
    }
}
