using System.Security.Cryptography;
using System.Text;
using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DirectKarigar.Infrastructure.Persistence;

public static class DbInitializer
{
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return; // DB already seeded
        }

        // 1. Create Demo Customer (Aarav Patel)
        var customerUser = new User
        {
            Id = "user_cust_aarav",
            Name = "Aarav Patel",
            Email = "aarav.customer@karigardirect.in",
            Phone = "+91 98450 21980",
            PasswordHash = HashPassword("Customer@123"),
            Role = UserRole.Customer,
            AvatarBg = "bg-emerald-700",
            Initials = "AP",
            Locality = "Indiranagar 12th Main",
            City = "Bengaluru",
            CustomerProfile = new CustomerProfile
            {
                Id = "cust_aarav",
                City = "Bengaluru",
                Locality = "Indiranagar 12th Main",
                Lat = 12.9784,
                Lng = 77.6408,
                InternalProRatingSum = 49.0,
                InternalProRatingCount = 10,
                InternalCancellationRate = 0.03,
                InternalPaymentReliability = 98.0
            }
        };

        // 2. Create Demo Pro (Ramesh Sharma)
        var proUser = new User
        {
            Id = "user_pro_ramesh",
            Name = "Ramesh Sharma",
            Email = "ramesh.pro@karigardirect.in",
            Phone = "+91 94480 32155",
            PasswordHash = HashPassword("Karigar@123"),
            Role = UserRole.Professional,
            AvatarBg = "bg-amber-600",
            Initials = "RS",
            Locality = "Indiranagar & Domlur",
            City = "Bengaluru",
            ProfessionalProfile = new ProfessionalProfile
            {
                Id = "pro_ramesh",
                Trade = "Electrician & Wireman",
                ExperienceYears = 14,
                City = "Bengaluru",
                Locality = "Indiranagar & Domlur",
                Lat = 12.9719,
                Lng = 77.6412,
                Verified = true,
                PublicRating = 4.9,
                TotalJobsDone = 842,
                ActiveOrdersCount = 1,
                MaxConcurrentOrders = 3,
                UpiId = "ramesh.sharma@okaxis",
                PenaltyPoints = 0,
                TrustScore = 99,
                Reviews = new List<Review>
                {
                    new()
                    {
                        ReviewerName = "Priya Sundaram",
                        Rating = 5.0,
                        Comment = "Ramesh arrived within 25 mins, fixed heavy Havells fan and regulator without charging extra."
                    },
                    new()
                    {
                        ReviewerName = "Karthik Rao",
                        Rating = 5.0,
                        Comment = "Direct negotiation was smooth. Fixed MCB trip issue for ₹350 genuine price."
                    }
                }
            }
        };

        // 3. Create Demo Admin (Vikram Malhotra)
        var adminUser = new User
        {
            Id = "user_admin_vikram",
            Name = "Vikram Malhotra",
            Email = "ops.admin@karigardirect.in",
            Phone = "+91 80234 56789",
            PasswordHash = HashPassword("Admin@123"),
            Role = UserRole.Admin,
            AdminRole = AdminRole.SuperAdmin,
            AvatarBg = "bg-stone-900",
            Initials = "VM",
            Locality = "HQ - Indiranagar",
            City = "Bengaluru"
        };

        // 4. Additional Bengaluru Pros for Radar
        var proSuresh = new User
        {
            Id = "user_pro_suresh",
            Name = "Suresh Kumar",
            Email = "suresh.pro@karigardirect.in",
            Phone = "+91 97412 88410",
            PasswordHash = HashPassword("Karigar@123"),
            Role = UserRole.Professional,
            AvatarBg = "bg-cyan-700",
            Initials = "SK",
            Locality = "Koramangala & HSR",
            City = "Bengaluru",
            ProfessionalProfile = new ProfessionalProfile
            {
                Id = "pro_suresh",
                Trade = "Plumber & Sanitary Specialist",
                ExperienceYears = 9,
                City = "Bengaluru",
                Locality = "Koramangala & HSR",
                Lat = 12.9352,
                Lng = 77.6245,
                Verified = true,
                PublicRating = 4.8,
                TotalJobsDone = 530,
                ActiveOrdersCount = 1,
                MaxConcurrentOrders = 4,
                UpiId = "sureshkumar.plumb@icici",
                PenaltyPoints = 0,
                TrustScore = 96
            }
        };

        context.Users.AddRange(customerUser, proUser, adminUser, proSuresh);
        await context.SaveChangesAsync();

        // 5. Seed KYC Records
        var kycRecord1 = new KycRecord
        {
            Id = "kyc_rec_001",
            ProfessionalId = "pro_ramesh",
            ProfessionalName = "Ramesh Sharma",
            Trade = "Electrician & Wireman",
            Phone = "+91 94480 32155",
            Locality = "Indiranagar & Domlur",
            City = "Bengaluru",
            AadhaarNumberMasked = "XXXX-XXXX-9901",
            PanNumberMasked = "BLSPS****M",
            Status = KycVerificationStatus.Verified,
            SelfieUrl = "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=400&q=80",
            ReviewedByAdminId = "user_admin_vikram",
            ReviewedByAdminRole = AdminRole.KycVerifier,
            ReviewedAtUtc = DateTime.UtcNow.AddDays(-10),
            FaceMatchConfidence = 98.4,
            TamperDetectionScore = 99.8,
            AadhaarOtpValidated = true,
            DuplicateAccountRisk = "low",
            Documents = new List<KycDocument>
            {
                new()
                {
                    Type = KycDocumentType.AadhaarCard,
                    Title = "UIDAI Aadhaar Card",
                    DocumentNumberMasked = "XXXX-XXXX-9901",
                    FileUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?auto=format&fit=crop&w=800&q=80"
                },
                new()
                {
                    Type = KycDocumentType.ItiTradeCertificate,
                    Title = "National Trade Certificate (NTC) - Wireman",
                    DocumentNumberMasked = "NTC/KA/2012/8812",
                    FileUrl = "https://images.unsplash.com/photo-1589829545856-d10d557cf95f?auto=format&fit=crop&w=800&q=80"
                }
            }
        };

        context.KycRecords.Add(kycRecord1);

        // 6. Seed Initial Jobs
        var job1 = new Job
        {
            Id = "job_01",
            Title = "Living Room Ceiling Fan Regulator & MCB Sparking",
            Category = ServiceCategory.Electrician,
            Description = "Atomberg inverter fan regulator knob is loose and causing periodic tripping at the main distribution board.",
            City = "Bengaluru",
            Locality = "Indiranagar 12th Main",
            Lat = 12.9784,
            Lng = 77.6408,
            BudgetMin = 300,
            BudgetMax = 500,
            AgreedPrice = 400,
            Urgency = "immediate",
            Status = JobStatus.Assigned,
            CustomerId = "cust_aarav",
            CustomerName = "Aarav Patel",
            CustomerLocality = "Indiranagar 12th Main",
            AssignedProId = "pro_ramesh",
            AssignedProName = "Ramesh Sharma",
            AssignedProPhone = "+91 94480 32155",
            Messages = new List<ChatMessage>
            {
                new()
                {
                    SenderId = "system",
                    SenderRole = "system",
                    SenderName = "KarigarDirect Radar",
                    Text = "Requirement broadcasted to verified local electricians within 3km. Zero middleman commission applied."
                },
                new()
                {
                    SenderId = "pro_ramesh",
                    SenderRole = "professional",
                    SenderName = "Ramesh Sharma",
                    Text = "Namaskara! I am at Domlur signal, can reach in 15 mins. Can inspect regulator & MCB for ₹400.",
                    IsOffer = true,
                    OfferAmount = 400,
                    OfferStatus = OfferStatus.Accepted
                },
                new()
                {
                    SenderId = "cust_aarav",
                    SenderRole = "customer",
                    SenderName = "Aarav Patel",
                    Text = "Deal, Ramesh ji. Please come over. Locality gate 2 is open."
                }
            }
        };

        var job2 = new Job
        {
            Id = "job_02",
            Title = "Kitchen Dual-Handle Sink Faucet Leaking Under Slab",
            Category = ServiceCategory.Plumber,
            Description = "Water pooling under kitchen cabinet whenever main supply is on. Need washer replacement or tap body resealing.",
            City = "Bengaluru",
            Locality = "HAL 2nd Stage",
            Lat = 12.9642,
            Lng = 77.6521,
            BudgetMin = 250,
            BudgetMax = 450,
            Urgency = "today",
            Status = JobStatus.Posted,
            CustomerId = "cust_aarav",
            CustomerName = "Aarav Patel",
            CustomerLocality = "Indiranagar 12th Main",
            Messages = new List<ChatMessage>
            {
                new()
                {
                    SenderId = "system",
                    SenderRole = "system",
                    SenderName = "KarigarDirect Radar",
                    Text = "Requirement broadcasted to verified local plumbers within 3km of HAL 2nd Stage."
                }
            }
        };

        context.Jobs.AddRange(job1, job2);

        // 7. Seed Locality Demand / Supply Metrics
        context.LocalityDemandSupplies.AddRange(
            new LocalityDemandSupply { Locality = "Indiranagar", ActivePros = 18, OpenJobsCount = 4, HealthStatus = "optimal", AvgResponseTimeMinutes = 3, SurgeMultiplier = 1.0 },
            new LocalityDemandSupply { Locality = "Koramangala", ActivePros = 24, OpenJobsCount = 8, HealthStatus = "deficit", AvgResponseTimeMinutes = 5, SurgeMultiplier = 1.15 },
            new LocalityDemandSupply { Locality = "Domlur", ActivePros = 9, OpenJobsCount = 2, HealthStatus = "surplus", AvgResponseTimeMinutes = 4, SurgeMultiplier = 1.0 },
            new LocalityDemandSupply { Locality = "HAL & Old Airport", ActivePros = 11, OpenJobsCount = 3, HealthStatus = "optimal", AvgResponseTimeMinutes = 4, SurgeMultiplier = 1.0 }
        );

        // 8. Seed Admin Audit Logs
        context.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminName = "Vikram Malhotra",
            AdminRole = AdminRole.SuperAdmin,
            Action = "SYSTEM_INITIALIZED",
            TargetId = "ALL",
            TargetType = "locality_rule",
            Details = "DirectKarigar Bengaluru cluster initialized with verified Karigar credentials and zero-commission fee structure.",
            BadgeColor = "emerald"
        });

        await context.SaveChangesAsync();
    }
}
