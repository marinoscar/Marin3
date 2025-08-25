using MarinApp.Agents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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

        public override async Task<AgentMessage> SendMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default)
        {
            var result = await WaitOnHumanResponseAsync(content.Content, this.History, cancellationToken);
            var response = new AgentMessage()
            {
                AgentId = this.Id,
                SessionId = SessionId,
                Role = "Human",
                Content = result,
                MimeType = "text/markdown",
                AgentName = this.Name,
                InnerContent = result,
                ModelId = "human-proxy",
                Metadata = GetMetadata(result)
            };
            return response;
        }

        protected virtual string GetMetadata(string humanResponse)
        {
            return "{}";
        }

        public override Task<AgentMessage> StreamMessageAsync(ChatMessageContent content, Action<StreamingChatMessageContent> onResponse, PromptExecutionSettings? executionSettings = default, CancellationToken cancellationToken = default)
        {
            return SendMessageAsync(content, executionSettings ?? DefaultExecutionSettings, cancellationToken);
        }

        public abstract Task<string> WaitOnHumanResponseAsync(string? agentText, ChatHistory history, CancellationToken cancellationToken);
    }
}
