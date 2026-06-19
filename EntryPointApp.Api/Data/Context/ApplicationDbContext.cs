using EntryPointApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Data.Context
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Timesheet_Users { get; set; }
        public DbSet<RefreshToken> Timesheet_RefreshTokens { get; set; }
        public DbSet<WeeklyLog> Timesheet_WeeklyLogs { get; set; }
        public DbSet<DailyLog> Timesheet_DailyLogs { get; set; }
        public DbSet<UserRate> Timesheet_UserRates { get; set; }
        public DbSet<DailyLogAttachment> Timesheet_DailyLogAttachments { get; set; }
        public DbSet<PayrollSchedule> Timesheet_PayrollSchedules { get; set; }
        public DbSet<ApprovedEmail> Timesheet_ApprovedEmails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUser(modelBuilder);
            ConfigureRefreshToken(modelBuilder);
            ConfigureWeeklyLog(modelBuilder);
            ConfigureDailyLog(modelBuilder);
            ConfigureUserRate(modelBuilder);
            ConfigureDailyLogAttachment(modelBuilder);
            ConfigurePayrollSchedule(modelBuilder);
            ConfigureApprovedEmail(modelBuilder);
        }

        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).HasConversion<string>();
                entity.Property(e => e.EmployeeType).HasConversion<string>().IsRequired(false);
                entity.Property(e => e.PasswordResetToken).HasMaxLength(256);
                entity.HasIndex(e => e.PasswordResetToken);
                entity.HasOne(e => e.Manager)
                    .WithMany(m => m.ManagedUsers)
                    .HasForeignKey(e => e.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ManagerId);
                entity.HasOne(e => e.SalesRep)
                    .WithMany(s => s.AssignedClients)
                    .HasForeignKey(e => e.SalesRepId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.SalesRepId);
                entity.HasIndex(e => e.Role);
            });
        }

        private void ConfigureRefreshToken(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ExpiryDate).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureWeeklyLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WeeklyLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.DateFrom).IsRequired();
                entity.Property(e => e.DateTo).IsRequired();
                entity.Property(e => e.TotalHours).HasPrecision(5, 2);
                entity.Property(e => e.TotalCharges).HasPrecision(8, 2);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                entity.Property(e => e.SalesRepComment)
                    .HasMaxLength(500);
                entity.Property(e => e.ManagerComment)
                    .HasMaxLength(500);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.WeeklyLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.DateFrom, e.DateTo });
                entity.HasIndex(e => e.Status);
            });
        }

        private void ConfigureDailyLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DailyLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.WeeklyLogId).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Mileage).HasPrecision(8, 2);
                entity.Property(e => e.TollCharge).HasPrecision(8, 2);
                entity.Property(e => e.ParkingFee).HasPrecision(8, 2);
                entity.Property(e => e.OtherCharges).HasPrecision(8, 2);
                entity.Property(e => e.Comment).HasMaxLength(500);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.DailyLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.WeeklyLog)
                    .WithMany(w => w.DailyLogs)
                    .HasForeignKey(e => e.WeeklyLogId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.WeeklyLogId);
                entity.HasIndex(e => new { e.WeeklyLogId, e.Date });
            });
        }

        private void ConfigureUserRate(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.HourlyRate).HasPrecision(18, 4).IsRequired();
                entity.Property(e => e.MileageRate).HasPrecision(18, 4).IsRequired();
                entity.Property(e => e.EffectiveDate).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Rates)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.CreatedByAdmin)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.EffectiveDate });
            });
        }

        private void ConfigureDailyLogAttachment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DailyLogAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(260);
                entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(260);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.DailyLog)
                    .WithMany(d => d.Attachments)
                    .HasForeignKey(e => e.DailyLogId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.DailyLogId);
            });
        }

        private void ConfigurePayrollSchedule(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PayrollSchedule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DateFrom).IsRequired();
                entity.Property(e => e.DateTo).IsRequired();
                entity.Property(e => e.PayrollDate).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasIndex(e => e.DateFrom);
            });
        }

        private void ConfigureApprovedEmail(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApprovedEmail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasOne(e => e.AddedByAdmin)
                    .WithMany()
                    .HasForeignKey(e => e.AddedByAdminId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}