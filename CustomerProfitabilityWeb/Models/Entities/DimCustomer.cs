using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerProfitabilityWeb.Models.Entities
{
    [Table("DimCustomer")]
    public class DimCustomer
    {
        [Key]
        public int CustomerKey { get; set; }

        [Required]
        [StringLength(50)]
        public string CustomerID { get; set; }

        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(50)]
        public string Region { get; set; }

        [Required]
        [StringLength(100)]
        public string Province { get; set; }

        [StringLength(100)]
        public string District { get; set; }

        [Required]
        [StringLength(100)]
        public string Industry { get; set; }

        [Required]
        [StringLength(100)]
        public string ExecutiveName { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

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