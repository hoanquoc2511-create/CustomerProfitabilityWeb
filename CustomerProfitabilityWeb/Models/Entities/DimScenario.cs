using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerProfitabilityWeb.Models.Entities
{
    [Table("DimScenario")]
    public class DimScenario
    {
        [Key]
        public int ScenarioKey { get; set; }

        [Required]
        [StringLength(50)]
        public string ScenarioName { get; set; }

        [StringLength(200)]
        public string ScenarioDescription { get; set; }
    }
}