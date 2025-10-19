using EntryPointApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Data.Context
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<WeeklyLog> WeeklyLogs { get; set; }
        public DbSet<DailyLog> DailyLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUser(modelBuilder);
            ConfigureRefreshToken(modelBuilder);
            ConfigureWeeklyLog(modelBuilder);
            ConfigureDailyLog(modelBuilder);
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
                entity.HasOne(e => e.Manager)
                    .WithMany(m => m.ManagedUsers)
                    .HasForeignKey(e => e.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ManagerId);
                entity.HasIndex(e => e.IsManager);
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
               entity.Property(e => e.TollCharges).HasPrecision(8, 2);
               entity.Property(e => e.Status).HasMaxLength(50);
               entity.HasOne(e => e.User)
                   .WithMany(u => u.WeeklyLogs)
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
               entity.HasIndex(e => e.UserId);
               entity.HasIndex(e => new { e.UserId, e.DateFrom, e.DateTo });
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
                entity.Property(e => e.Hours).HasPrecision(5, 2);
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
    }
}