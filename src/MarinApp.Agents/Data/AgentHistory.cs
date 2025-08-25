using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Data
{
    public class AgentHistory : List<AgentItem>
    {
        public ChatHistory ChatHistory { get; set; } = new();
    }
}
