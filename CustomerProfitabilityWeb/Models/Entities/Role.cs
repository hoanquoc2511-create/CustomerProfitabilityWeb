using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerProfitabilityWeb.Models.Entities
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }

        [StringLength(200)]
        public string RoleDescription { get; set; }

        public bool CanUploadData { get; set; }
        public bool CanViewAllData { get; set; }
        public bool CanDeleteData { get; set; }
        public bool CanManageUsers { get; set; }
        public bool CanUseAI { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}