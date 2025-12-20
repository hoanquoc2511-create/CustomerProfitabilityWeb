using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerProfitabilityWeb.Models.Entities
{
    [Table("UploadBatch")]
    public class UploadBatch
    {
        [Key]
        public int BatchID { get; set; }

        [StringLength(200)]
        public string BatchName { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        public long? FileSize { get; set; }

        [StringLength(500)]
        public string FilePath { get; set; }

        public int UploadedBy { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        // Statistics
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalTransactions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRevenue { get; set; }

        // Status
        [StringLength(50)]
        public string Status { get; set; } = "Processing";

        public string ErrorMessage { get; set; }
        public int? ProcessingTime { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        [ForeignKey("UploadedBy")]
        public virtual User User { get; set; }
    }
}