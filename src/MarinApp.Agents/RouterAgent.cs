using MarinApp.Agents.Data;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MarinApp.Agents
{
    public class RouterAgent : OpenAIAgentBase
    {
        public RouterAgent(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory, IConfiguration configuration) : base(agentHistoryService, loggerFactory, configuration)
        {
            SetAgentDetails("router-agent", "Router Agent", "An AI agent that routes user requests to the appropriate specialized agent based on the content of the request.");
            ModelId = "gpt-4o";
            var schemaDefinition = """
{
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "next":       { "type": "string" },
    "rationale":  { "type": "string" },
    "confidence": { "type": "number", "minimum": 0, "maximum": 1 }
  },
  "required": ["next","rationale","confidence"]
}
""";
            var json = JsonDocument.Parse(schemaDefinition);
            DefaultExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(json.RootElement),
                User = Name,
                
            };
            SetSystemMessage(@"You are a router agent that directs user requests to the appropriate specialized agent.");
        }
    }
}
