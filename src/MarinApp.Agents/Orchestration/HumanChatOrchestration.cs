using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Orchestration
{
    public class HumanChatOrchestration
    {
        protected HumanProxyBase HumanAgent { get; private set; } = default!;
        protected IAgent Agent { get; private set; } = default!;

        public HumanChatOrchestration(HumanProxyBase humanAgent, IAgent agent)
        {
            HumanAgent = humanAgent ?? throw new ArgumentNullException(nameof(humanAgent));
            Agent = agent ?? throw new ArgumentNullException(nameof(agent));
            var session = Guid.NewGuid().ToString();
            HumanAgent.SetSession(session);
            Agent.SetSession(session);
        }

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            var agentResponse = await Agent.SendMessageAsync(message, new PromptExecutionSettings(), cancellationToken);
        }

    }
}
