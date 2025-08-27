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
        [Description("The name  of the agent that should go next")]
        [Required]
        public string Next { get; set; } = default!;

        [Description("Brief description as to why the next agent is selected or what the task is completed")]
        [MaxLength(240)]
        [Required]
        public string Rationale { get; set; } = default!;

        [Range(0, 1)]
        [Required]
        public double Confidence { get; set; }

        [Description("Set to true when you feel the goal is completed.")]
        [Required]
        public bool GoalCompleted { get; set; }

        }
}
