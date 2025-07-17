using EntryPointApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Data.Context
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<WeeklyLog> WeeklyLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUser(modelBuilder);
            ConfigurationRefreshToken(modelBuilder);
            ConfigurationWeeklyLog(modelBuilder);
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

        private void ConfigurationRefreshToken(ModelBuilder modelBuilder)
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

        private void ConfigurationWeeklyLog(ModelBuilder modelBuilder)
        {
             modelBuilder.Entity<WeeklyLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Hours).HasPrecision(5, 2);
                entity.Property(e => e.Mileage).HasPrecision(8, 2);
                entity.Property(e => e.TollCharge).HasPrecision(8, 2);
                entity.Property(e => e.ParkingFee).HasPrecision(8, 2);
                entity.Property(e => e.OtherCharges).HasPrecision(8, 2);
                entity.Property(e => e.Comment).HasMaxLength(500);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.WeeklyLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}