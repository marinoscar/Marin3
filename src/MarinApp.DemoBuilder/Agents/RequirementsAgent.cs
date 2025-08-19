using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.Extensions.Logging;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.DemoBuilder.Agents
{
    public class RequirementsAgent : BaseAgent
    {

        private readonly OpenAIChatAgent _chatAgent;

        public RequirementsAgent(AgentInfo agentInfo, ILogger<RequirementsAgent> logger)
            : base(agentInfo, logger)
        {
            var client = new OpenAIClient(
                apiKey: agentInfo.ApiKey
            );

            _chatAgent = new OpenAIChatAgent(
                client.GetChatClient("gpt-4o"), agentInfo.Name, agentInfo.SystemPrompt
            );
            _chatAgent.RegisterPrintMessage();
            _chatAgent.RegisterPrintFormatMessageHook(
                (message) =>
                {
                    logger.LogInformation("Chat Message: {Message}", message);
                }
            );
        }


    }
}
