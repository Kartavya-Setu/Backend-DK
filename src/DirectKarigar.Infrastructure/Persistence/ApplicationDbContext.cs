using DirectKarigar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DirectKarigar.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<ProfessionalProfile> ProfessionalProfiles => Set<ProfessionalProfile>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<WorkBill> WorkBills => Set<WorkBill>();
    public DbSet<DisputeCase> DisputeCases => Set<DisputeCase>();
    public DbSet<KycRecord> KycRecords => Set<KycRecord>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<LocalityDemandSupply> LocalityDemandSupplies => Set<LocalityDemandSupply>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User relationships
        modelBuilder.Entity<User>()
            .HasOne(u => u.CustomerProfile)
            .WithOne(c => c.User)
            .HasForeignKey<CustomerProfile>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.ProfessionalProfile)
            .WithOne(p => p.User)
            .HasForeignKey<ProfessionalProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Job relationships
        modelBuilder.Entity<Job>()
            .HasOne(j => j.Customer)
            .WithMany(c => c.PostedJobs)
            .HasForeignKey(j => j.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Job>()
            .HasOne(j => j.AssignedPro)
            .WithMany(p => p.AssignedJobs)
            .HasForeignKey(j => j.AssignedProId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Job>()
            .HasOne(j => j.Bill)
            .WithOne(b => b.Job)
            .HasForeignKey<WorkBill>(b => b.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Job>()
            .HasMany(j => j.Messages)
            .WithOne(m => m.Job)
            .HasForeignKey(m => m.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optimistic concurrency token
        modelBuilder.Entity<Job>()
            .Property(j => j.RowVersion)
            .IsRowVersion();

        // KYC relationships
        modelBuilder.Entity<ProfessionalProfile>()
            .HasOne(p => p.KycRecord)
            .WithOne(k => k.Professional)
            .HasForeignKey<KycRecord>(k => k.ProfessionalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KycRecord>()
            .HasMany(k => k.Documents)
            .WithOne(d => d.KycRecord)
            .HasForeignKey(d => d.KycRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        // Review relationships
        modelBuilder.Entity<ProfessionalProfile>()
            .HasMany(p => p.Reviews)
            .WithOne(r => r.ProfessionalProfile)
            .HasForeignKey(r => r.ProfessionalProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
