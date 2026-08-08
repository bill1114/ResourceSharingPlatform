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
        public DbSet<AIStockInSettings> AIStockInSettings { get; set; }
        public DbSet<AIStockInLog> AIStockInLogs { get; set; }
        public DbSet<InventoryItemDefinition> InventoryItemDefinitions { get; set; }
        public DbSet<InventoryItemVariant> InventoryItemVariants { get; set; }
        public DbSet<LocationInventorySafetyStock> LocationInventorySafetyStocks { get; set; }

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
            modelBuilder.Entity<AIStockInSettings>().ToTable("AIStockInSettings");
            modelBuilder.Entity<AIStockInLog>().ToTable("AIStockInLog");
            modelBuilder.Entity<InventoryItemDefinition>().ToTable("InventoryItemDefinition");
            modelBuilder.Entity<InventoryItemVariant>().ToTable("InventoryItemVariant");
            modelBuilder.Entity<LocationInventorySafetyStock>().ToTable("LocationInventorySafetyStock");

            // Configure relationships
            modelBuilder.Entity<SupplyItem>()
                .HasOne(x => x.Location)
                .WithMany(x => x.SupplyItems)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupplyItem>()
                .HasOne(x => x.InventoryItemVariant)
                .WithMany()
                .HasForeignKey(x => x.InventoryItemVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryItemDefinition>()
                .HasIndex(x => new { x.Category, x.ItemName })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            modelBuilder.Entity<InventoryItemVariant>()
                .HasOne(x => x.InventoryItemDefinition)
                .WithMany(x => x.Variants)
                .HasForeignKey(x => x.InventoryItemDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryItemVariant>()
                .HasIndex(x => new { x.InventoryItemDefinitionId, x.Specification })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            modelBuilder.Entity<LocationInventorySafetyStock>()
                .HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LocationInventorySafetyStock>()
                .HasOne(x => x.InventoryItemDefinition)
                .WithMany(x => x.LocationSafetyStocks)
                .HasForeignKey(x => x.InventoryItemDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LocationInventorySafetyStock>()
                .HasIndex(x => new { x.LocationId, x.InventoryItemDefinitionId })
                .IsUnique();

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

            modelBuilder.Entity<AIStockInLog>()
                .HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AIStockInLog>()
                .HasOne(x => x.ConfirmedSupplyItem)
                .WithMany()
                .HasForeignKey(x => x.ConfirmedSupplyItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AIStockInLog>()
                .Property(x => x.Confidence)
                .HasPrecision(5, 4);

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
