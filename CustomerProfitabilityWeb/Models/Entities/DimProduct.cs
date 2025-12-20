using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerProfitabilityWeb.Models.Entities
{
    [Table("DimProduct")]
    public class DimProduct
    {
        [Key]
        public int ProductKey { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; }

        [StringLength(50)]
        public string BU { get; set; }

        [Required]
        [StringLength(50)]
        public string Division { get; set; }

        [StringLength(50)]
        public string Industry { get; set; }

        public bool IsActive { get; set; } = true;

        // Import info
        public int? BatchID { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }

        // Navigation
        [ForeignKey("BatchID")]
        public virtual UploadBatch UploadBatch { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User User { get; set; }
    }
}