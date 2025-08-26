using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Orchestration
{
    public class RouteDecision
    {
        [Description("Next step the system should take.")]
        [Required]
        public string Next { get; set; } = default!;

        [MaxLength(240)]
        [Required]
        public string Rationale { get; set; } = default!;

        [Range(0, 1)]
        [Required]
        public double Confidence { get; set; }
    }
}
