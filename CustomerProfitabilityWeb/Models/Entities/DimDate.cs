using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerProfitabilityWeb.Models.Entities
{
    [Table("DimDate")]
    public class DimDate
    {
        [Key]
        public int DateKey { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        [StringLength(20)]
        public string MonthName { get; set; }

        public int Quarter { get; set; }

        [StringLength(10)]
        public string QuarterName { get; set; }

        [StringLength(10)]
        public string YearMonth { get; set; }

        [StringLength(20)]
        public string MonthYear { get; set; }
    }
}