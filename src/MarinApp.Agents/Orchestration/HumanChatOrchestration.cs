using MarinApp.Agents.Data;
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
        protected IHumanProxy HumanAgent { get; private set; } = default!;
        protected IAgent Agent { get; private set; } = default!;

        public HumanChatOrchestration(HumanProxyBase humanAgent, IAgent agent)
        {
            HumanAgent = humanAgent ?? throw new ArgumentNullException(nameof(humanAgent));
            Agent = agent ?? throw new ArgumentNullException(nameof(agent));
            var session = Guid.NewGuid().ToString();
            HumanAgent.SetSession(session);
            Agent.SetSession(session);
        }

        public void StartChat(string initialMessage, Func<AgentMessage, bool> endSequence)
        {
            if (string.IsNullOrEmpty(initialMessage)) throw new ArgumentNullException(nameof(initialMessage));

            while (true)
            {
                var humanResponse = HumanAgent.SendMessageAsync(initialMessage).GetAwaiter().GetResult();
                var agentResponse = Agent.SendMessageAsync(humanResponse.Content).GetAwaiter().GetResult();
                //merge the history
            }

        }

    }
}
