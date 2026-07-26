using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Models;

namespace ResourceSharingPlatform.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SupplyLocation> SupplyLocations { get; set; }
        public DbSet<SupplyItem> SupplyItems { get; set; }
        public DbSet<SupplyTransferLog> SupplyTransferLogs { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<SupplyOutboundLog> SupplyOutboundLogs { get; set; }
        public DbSet<SupplyDonationLog> SupplyDonationLogs { get; set; }
        public DbSet<LineNotificationSettings> LineNotificationSettings { get; set; }
        public DbSet<SupplyDisposalLog> SupplyDisposalLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names
            modelBuilder.Entity<SupplyLocation>().ToTable("SupplyLocation");
            modelBuilder.Entity<SupplyItem>().ToTable("SupplyItem");
            modelBuilder.Entity<SupplyTransferLog>().ToTable("SupplyTransferLog");
            modelBuilder.Entity<UserAccount>().ToTable("UserAccount");
            modelBuilder.Entity<SupplyOutboundLog>().ToTable("SupplyOutboundLog");
            modelBuilder.Entity<SupplyDonationLog>().ToTable("SupplyDonationLog");
            modelBuilder.Entity<LineNotificationSettings>().ToTable("LineNotificationSettings");
            modelBuilder.Entity<SupplyDisposalLog>().ToTable("SupplyDisposalLog");

            // Configure relationships
            modelBuilder.Entity<SupplyItem>()
                .HasOne(x => x.Location)
                .WithMany(x => x.SupplyItems)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyTransferLog>()
                .HasOne(x => x.SupplyItem)
                .WithMany()
                .HasForeignKey(x => x.SupplyItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyTransferLog>()
                .HasOne(x => x.FromLocation)
                .WithMany()
                .HasForeignKey(x => x.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyTransferLog>()
                .HasOne(x => x.ToLocation)
                .WithMany()
                .HasForeignKey(x => x.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyOutboundLog>()
                .HasOne(x => x.SupplyItem)
                .WithMany()
                .HasForeignKey(x => x.SupplyItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyOutboundLog>()
                .HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserAccount>()
                .HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyDonationLog>()
                .HasOne(x => x.SupplyItem)
                .WithMany()
                .HasForeignKey(x => x.SupplyItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyDonationLog>()
                .HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyDisposalLog>()
                .HasOne(x => x.SupplyItem)
                .WithMany()
                .HasForeignKey(x => x.SupplyItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyDisposalLog>()
                .HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision
            modelBuilder.Entity<SupplyLocation>()
                .Property(x => x.Latitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<SupplyLocation>()
                .Property(x => x.Longitude)
                .HasPrecision(10, 7);
        }
    }
}
