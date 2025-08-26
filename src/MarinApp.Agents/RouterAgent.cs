using MarinApp.Agents.Data;
using MarinApp.Agents.Orchestration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
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
            Initialize();
        }

        public IHumanProxy HumanAgent { get; private set; }
        public IList<IAgent> SpecializedAgents { get; private set; }

        public bool IsInitialized => HumanAgent != null && SpecializedAgents != null && SpecializedAgents.Count > 0 && string.IsNullOrEmpty(SystemPrompt);

        protected virtual void Initialize()
        {
            SetAgentDetails("router-agent", "Router Agent", "An AI agent that routes user requests to the appropriate specialized agent based on the content of the request.");
            ModelId = "gpt-4o";
            var schema = JsonSchemaGenerator.Generate<RouteDecision>();
            DefaultExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(schema.RootElement),
                User = Name,
            };
        }

        public void InitializeAgents(IHumanProxy humanProxy, params IAgent[] agents)
        {
            InitializeAgents(CreateSystemPrompt(), humanProxy, agents);
        }

        public void InitializeAgents(string routerAgentPrompt, IHumanProxy humanProxy, params IAgent[] agents)
        {
            if (string.IsNullOrWhiteSpace(routerAgentPrompt)) throw new ArgumentNullException(nameof(routerAgentPrompt));
            HumanAgent = humanProxy ?? throw new ArgumentNullException(nameof(humanProxy));
            SpecializedAgents = agents?.ToList() ?? throw new ArgumentNullException(nameof(agents));
            SetSystemMessage(routerAgentPrompt);
            var sessionId = Guid.NewGuid().ToString("N");
            InitializeSession(sessionId);
        }

        private void InitializeSession(string sessionId)
        {
            HumanAgent.SetSession(sessionId);
            foreach (var agent in SpecializedAgents)
            {
                agent.SetSession(sessionId);
            }
        }

        public async Task SetGoalAsync(string goal, int maxInterations = 32, CancellationToken cancellationToken = default)
        {
            if(!IsInitialized) throw new InvalidOperationException("RouterAgent is not initialized. Call InitializeAgents() first.");
            var agentHistory = new AgentHistory();

            var routerDecision = await MessageRouterAsync(goal, cancellationToken);
            for (int i = 0; i < maxInterations; i++)
            {

                if (decision.AgentId == HumanAgent.Id)
                {
                    await HumanAgent.SendMessageAsync(new ChatMessageContent(goal), DefaultExecutionSettings, cancellationToken);
                    return;
                }
                var agent = SpecializedAgents.FirstOrDefault(a => a.Id == decision.AgentId);
                if (agent == null)
                {
                    throw new InvalidOperationException($"No specialized agent found with ID '{decision.AgentId}'.");
                }
                var agentResponse = await agent.SendMessageAsync(new ChatMessageContent(goal), DefaultExecutionSettings, cancellationToken);
                goal = agentResponse.Content ?? string.Empty;
            }
        }

        protected virtual async Task<RouteDecision> MessageRouterAsync(string message, CancellationToken cancellationToken = default)
        {
            var res = await SendMessageAsync(message, DefaultExecutionSettings, cancellationToken);
            return ParseResponse(res.Content);
        }

        protected virtual RouteResponse ParseResponse(AgentMessage response)
        {
            try
            {
                var decision = JsonSerializer.Deserialize<RouteDecision>(response.Content);
                if (decision == null)
                {
                    throw new InvalidOperationException("Failed to parse the routing decision from the agent's response.");
                }
                return new RouteResponse();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse the routing decision from the agent's response.", ex);
            }
        }

        protected virtual string CreateSystemPrompt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a router agent that determines which specialized agent should handle a user's request.");
            sb.AppendLine("You have access to the following specialized agents:");
            foreach (var agent in SpecializedAgents)
            {
                sb.AppendLine($"- {agent.Name}: {agent.Description}");
            }
            sb.AppendLine("Based on the user's request, choose the most appropriate agent to handle it.");
            sb.AppendLine("If none of the specialized agents are suitable, route the request to the human agent.");
            sb.AppendLine("Provide your decision in the specified JSON format.");
            return sb.ToString();
        }

        protected record RouteResponse { AgentMessage Message; RouteDecision Decision; }


    }
}
