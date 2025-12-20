using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerProfitabilityWeb.Models.Entities
{
    [Table("FactSales")]
    public class FactSales
    {
        [Key]
        public long SalesKey { get; set; }

        // Foreign Keys
        public int DateKey { get; set; }
        public int ProductKey { get; set; }
        public int CustomerKey { get; set; }
        public int ExecutiveKey { get; set; }
        public int LocationKey { get; set; }
        public int ScenarioKey { get; set; }

        // Measures
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Revenue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal COGS { get; set; }

        // Computed columns (database-side)
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? GrossProfit { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? GrossProfitMarginPct { get; set; }

        // Metadata
        public int BatchID { get; set; }

        [StringLength(100)]
        public string TransactionID { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("DateKey")]
        public virtual DimDate DimDate { get; set; }

        [ForeignKey("ProductKey")]
        public virtual DimProduct DimProduct { get; set; }

        [ForeignKey("CustomerKey")]
        public virtual DimCustomer DimCustomer { get; set; }

        [ForeignKey("ExecutiveKey")]
        public virtual DimExecutive DimExecutive { get; set; }

        [ForeignKey("LocationKey")]
        public virtual DimLocation DimLocation { get; set; }

        [ForeignKey("ScenarioKey")]
        public virtual DimScenario DimScenario { get; set; }

        [ForeignKey("BatchID")]
        public virtual UploadBatch UploadBatch { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User User { get; set; }
    }
}