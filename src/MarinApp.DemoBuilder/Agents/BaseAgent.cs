using Azure.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.DemoBuilder.Agents
{
    public class BaseAgent
    {
        private readonly ILogger _logger;

        public BaseAgent(AgentInfo agentInfo, ILogger logger)
        {
            Info = agentInfo ?? throw new ArgumentNullException(nameof(agentInfo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public AgentInfo Info { get; private set; }


    }
}
