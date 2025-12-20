using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerProfitabilityWeb.Models.Entities
{
    [Table("DimExecutive")]
    public class DimExecutive
    {
        [Key]
        public int ExecutiveKey { get; set; }

        [StringLength(50)]
        public string ExecutiveID { get; set; }

        [Required]
        [StringLength(100)]
        public string ExecutiveName { get; set; }

        [StringLength(100)]
        public string ExecutiveTitle { get; set; }

        [StringLength(50)]
        public string Region { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        public bool IsActive { get; set; } = true;

        public int? BatchID { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

        [ForeignKey("BatchID")]
        public virtual UploadBatch UploadBatch { get; set; }
    }
}