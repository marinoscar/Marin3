using MarinApp.Agents.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents
{
    public class RouterAgent : OpenAIAgentBase
    {
        public RouterAgent(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory, IConfiguration configuration) : base(agentHistoryService, loggerFactory, configuration)
        {
            SetAgentDetails("router-agent", "Router Agent", "An AI agent that routes user requests to the appropriate specialized agent based on the content of the request.");
            ModelId = "gpt-4o";
            DefaultExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0
            };
            SetSystemMessage(@"You are a router agent that directs user requests to the appropriate specialized agent.");
        }
    }
}
