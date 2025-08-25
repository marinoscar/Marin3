using MarinApp.Agents.Data;
using Microsoft.Extensions.Configuration;
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
    /// <summary>
    /// Provides an abstract base class for agents that act as a proxy for human users in conversational scenarios.
    /// <para>
    /// This class implements <see cref="IAgent"/> and <see cref="IHumanProxy"/>, enabling integration with chat history,
    /// session management, and message handling. It is designed for scenarios where a human user is directly involved
    /// in the conversation loop, such as chat UIs or human-in-the-loop workflows.
    /// </para>
    /// <para>
    /// <b>Key Features:</b>
    /// <list type="bullet">
    ///   <item>
    ///     <description>Manages chat history and session context via <see cref="AgentBase"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>Implements message sending and streaming by awaiting human input.</description>
    ///   </item>
    ///   <item>
    ///     <description>Provides abstract methods for displaying messages and waiting for user responses.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Inherit from <see cref="HumanProxyBase"/> and implement <see cref="PrintUserMessage(string, string)"/>
    /// and <see cref="WaitOnHumanResponseAsync(string?, ChatHistory, CancellationToken)"/> to connect to your UI or input mechanism.
    /// </para>
    /// </summary>
    public abstract class HumanProxyBase : AgentBase, IAgent, IHumanProxy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HumanProxyBase"/> class.
        /// </summary>
        /// <param name="agentHistoryService">
        /// The service responsible for persisting and retrieving agent message history.
        /// </param>
        /// <param name="configuration">
        /// The application configuration instance.
        /// </param>
        /// <param name="loggerFactory">
        /// The logger factory used to create loggers for this agent instance.
        /// </param>
        protected HumanProxyBase(IAgentHistoryService agentHistoryService, IConfiguration configuration, ILoggerFactory loggerFactory) : base(agentHistoryService, configuration, loggerFactory)
        {
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Returns metadata for the human response, serialized as a JSON string.
        /// </summary>
        /// <param name="humanResponse">The response provided by the human user.</param>
        /// <returns>A JSON string representing metadata for the message. Default is an empty object ("{}").</returns>
        /// <remarks>
        /// Override this method to include additional metadata about the human response if needed.
        /// </remarks>
        protected virtual string GetMetadata(string humanResponse)
        {
            return "{}";
        }

        /// <inheritdoc/>
        public override Task<AgentMessage> StreamMessageAsync(ChatMessageContent content, Action<StreamingChatMessageContent> onResponse, PromptExecutionSettings? executionSettings = default, CancellationToken cancellationToken = default)
        {
            return SendMessageAsync(content, executionSettings ?? DefaultExecutionSettings, cancellationToken);
        }

        /// <inheritdoc/>
        public abstract void PrintUserMessage(string content, string mimeType);

        /// <inheritdoc/>
        public abstract void PrintAgentMessage(string content, string mimeType);

        /// <inheritdoc/>
        public abstract Task<string> WaitOnHumanResponseAsync(string? agentText, ChatHistory history, CancellationToken cancellationToken);
    }
}
