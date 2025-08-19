using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.DemoBuilder.Agents
{
    public class AgentInfo
    {
        public string ApiKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }

        public string SystemPrompt { get; set; }
    }
}
