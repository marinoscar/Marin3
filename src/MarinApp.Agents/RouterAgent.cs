using MarinApp.Agents.Data;
using MarinApp.Agents.Orchestration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public bool IsInitialized => HumanAgent != null && SpecializedAgents != null && SpecializedAgents.Count > 0 && !string.IsNullOrEmpty(SystemPrompt);

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
            InitializeAgents(null, humanProxy, agents);
        }

        public void InitializeAgents(string routerAgentPrompt, IHumanProxy humanProxy, params IAgent[] agents)
        {
            HumanAgent = humanProxy ?? throw new ArgumentNullException(nameof(humanProxy));
            SpecializedAgents = agents?.ToList() ?? throw new ArgumentNullException(nameof(agents));

            if (string.IsNullOrEmpty(routerAgentPrompt))
                routerAgentPrompt = CreateSystemPrompt();

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

            //Send the goal to the router agent to determine the next agent
            var routerDecision = await MessageRouterAsync(goal, cancellationToken);
            // Get the next agent to invoke
            var next = GetNext(routerDecision.Decision);
            
            //Initialize the common session history
            var agentHistory = new AgentHistory();

            //Establish the root message
            string prompt = goal;

            for (int i = 0; i < maxInterations; i++)
            {
                // set the history for the next agent
                next.Next.History.Clear();
                MergeHistory(next.Next.History, agentHistory);

                //Debug the transcript
                Debug.WriteLine(agentHistory.GetTranscript());

                // Notifies which agent is working
                HumanAgent.PrintStatusMessage($"Routing to agent: {next.Next.Name}. Rationale: {next.Rationale}", "text/markdown");

                // Send the message to the next agent, pass null exec settings to use the agent's default
                var agentResponse = await next.Next.SendMessageAsync(prompt, null, cancellationToken);

                // Add the agent response to the common history
                agentHistory.Add(new AgentItem() { 
                    Id = agentResponse.Id,
                    Content = agentResponse.MessageContent,
                    AgentMessage = agentResponse
                });

                // Merge the common history into the router from next agent's history
                MergeHistory(History, agentHistory);

                // Print the agent response
                HumanAgent.PrintAgentMessage(agentResponse.Content, agentResponse.MimeType);

                // Evaluate the next move
                routerDecision = await MessageRouterAsync("Decide on the next agent if the process is completed, just provide the word STOP on the Next property", cancellationToken);

                // Get's the next agent to invoke
                next = GetNext(routerDecision.Decision);

                if (next.Stop) break;



            }
        }

        protected virtual NextAgent GetNext(RouteDecision decision)
        {
            if (decision == null)
                throw new ArgumentNullException(nameof(decision));

            if(decision.GoalCompleted)
                return new NextAgent { Stop = true, Next = null, Rationale = "Task marked as completed." };

            if (string.IsNullOrEmpty(decision.Next))
                throw new InvalidOperationException($"{nameof(RouterAgent)} was unable to identify the next agent");

            if (decision.Next.ToLowerInvariant().Equals("stop") || decision.Next.ToLowerInvariant().Equals("exit"))
                return new NextAgent { Stop = true, Next = null, Rationale = "Stopping as per decision." };

            var nextAgent = SpecializedAgents.Single(a => a.Name.Equals(decision.Next, StringComparison.OrdinalIgnoreCase));
            return new NextAgent() { Stop = false, Next = nextAgent, Rationale = decision.Rationale };
        }

        protected virtual async Task<RouteResponse> MessageRouterAsync(string message, CancellationToken cancellationToken = default)
        {
            var res = await SendMessageAsync(message, DefaultExecutionSettings, cancellationToken);
            return ParseResponse(res);
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

        private void MergeHistory(AgentHistory target, AgentHistory source)
        {
            foreach (var message in source)
            {
                if (!target.Any(m => m.Id == message.Id))
                {
                    target.Add(message);
                }
            }
        }

        public record RouteResponse { public AgentMessage Message; public RouteDecision Decision; }

        public record NextAgent { public bool Stop; public IAgent Next; public string Rationale; }

    }
}
