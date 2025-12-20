using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerProfitabilityWeb.Models.Entities
{
    [Table("DimLocation")]
    public class DimLocation
    {
        [Key]
        public int LocationKey { get; set; }

        [Required]
        [StringLength(50)]
        public string Region { get; set; }

        [Required]
        [StringLength(100)]
        public string Province { get; set; }

        [StringLength(100)]
        public string District { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? Longitude { get; set; }
    }
}