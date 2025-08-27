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
    public class OpenAIAgentFactory
    {

        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _logger;
        private readonly IAgentHistoryService _agentHistoryService;

        public OpenAIAgentFactory(IAgentHistoryService agentHistoryService,
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _agentHistoryService = agentHistoryService ?? throw new ArgumentNullException(nameof(agentHistoryService));
            _logger = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public OpenAIAgentBase Create(string agentId, string name, string description, string systemPrompt, string modelId = "gpt-4o", OpenAIPromptExecutionSettings settings = default!)
        {
            var agent = new OpenAIAgentBase(_agentHistoryService, _logger, _configuration);
            agent.SetAgentDetails(agentId, name, description);
            agent.SetSystemMessage(systemPrompt);
            agent.ModelId = modelId;

            if (settings != null)
                agent.DefaultExecutionSettings = settings;

            return agent;
        }

        public OpenAIAgentBase Create(string name, string description, string systemPrompt, string modelId = "gpt-4o", OpenAIPromptExecutionSettings settings = default!)
        {
            return Create(name.ToLowerInvariant().Replace(" ", "_"), name, description, systemPrompt, modelId);  
        }
    }
}
