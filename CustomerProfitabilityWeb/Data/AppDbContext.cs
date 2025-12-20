using Microsoft.EntityFrameworkCore;
using CustomerProfitabilityWeb.Models.Entities;

namespace CustomerProfitabilityWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Metadata Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UploadBatch> UploadBatch { get; set; }

        // Dimension Tables
        public DbSet<DimProduct> DimProduct { get; set; }
        public DbSet<DimCustomer> DimCustomer { get; set; }
        public DbSet<DimExecutive> DimExecutive { get; set; }
        public DbSet<DimDate> DimDate { get; set; }
        public DbSet<DimLocation> DimLocation { get; set; }
        public DbSet<DimScenario> DimScenario { get; set; }

        // Fact Table
        public DbSet<FactSales> FactSales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // FactSales - Computed Columns
            modelBuilder.Entity<FactSales>()
                .Property(f => f.GrossProfit)
                .HasComputedColumnSql("[Revenue] - [COGS]", stored: true);

            modelBuilder.Entity<FactSales>()
                .Property(f => f.GrossProfitMarginPct)
                .HasComputedColumnSql("CASE WHEN [Revenue] > 0 THEN (([Revenue] - [COGS]) / [Revenue]) * 100 ELSE 0 END", stored: true);
        }
    }
}