using MarinApp.Agents.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents
{
    public abstract class HumanProxyBase : AgentBase
    {
        protected HumanProxyBase(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory) : base(agentHistoryService, loggerFactory)
        {
        }
    }
}
