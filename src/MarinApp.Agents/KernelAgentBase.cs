﻿using HandlebarsDotNet;
using MarinApp.Agents.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;
using System.Text.Json;

namespace MarinApp.Agents
{
    /// <summary>
    /// Provides a base class for conversational agents that interact with users via chat, manage session history, and interface with a semantic kernel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Usage:</b> Inherit from <see cref="KernelAgentBase"/> to implement a custom agent. Override <see cref="InitializeKernel"/> to provide a configured <see cref="Kernel"/> instance.
    /// </para>
    /// <para>
    /// <b>Session Management:</b> Use <see cref="StartSession"/> to begin a new session, or <see cref="SetSession"/> to resume an existing one. The <see cref="SessionId"/> property tracks the current session.
    /// </para>
    /// <para>
    /// <b>Message Streaming and Sending:</b> Use <see cref="StreamMessageAsync"/> for streaming responses and <see cref="SendMessageAsync"/> for single-turn responses.
    /// </para>
    /// <para>
    /// <b>History:</b> The <see cref="History"/> property contains the chat history for the current session. Use <see cref="RestoreHistoryAsync"/> to load previous messages.
    /// </para>
    /// <para>
    /// <b>System Prompts:</b> Set the system prompt using <see cref="SetSystemMessage"/>. Templates can be parsed with <see cref="ParseTemplate{T}"/>.
    /// </para>
    /// <para>
    /// <b>Events:</b> Subscribe to <see cref="MessageCompleted"/> to handle message completion events.
    /// </para>
    /// <example>
    /// <code>
    /// public class MyAgent : AgentBase
    /// {
    ///     protected override Kernel InitializeKernel()
    ///     {
    ///         // Configure and return a Kernel instance
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public abstract class KernelAgentBase : AgentBase, IAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KernelAgentBase"/> class.
        /// </summary>
        /// <param name="agentHistoryService">The service for persisting and retrieving agent message history.</param>
        /// <param name="configuration">The application's configuration settings.</param>
        /// <param name="loggerFactory">The logger factory for creating loggers.</param>
        /// <exception cref="InvalidOperationException">Thrown if the kernel cannot be initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown if required dependencies are null.</exception>
        public KernelAgentBase(IAgentHistoryService agentHistoryService, IConfiguration configuration, ILoggerFactory loggerFactory) : base(agentHistoryService, configuration, loggerFactory)
        {
        }

        /// <summary>
        /// When implemented in a derived class, initializes and returns the <see cref="Kernel"/> used for chat completion.
        /// </summary>
        /// <returns>The initialized <see cref="Kernel"/> instance.</returns>
        protected abstract Kernel InitializeKernel();

        /// <summary>
        /// Occurs when a message has been completed and processed by the agent.
        /// </summary>
        public event EventHandler<(ChatMessageContent MessageContent, AgentMessage AgentMessage)>? MessageCompleted;

        /// <summary>
        /// Gets the semantic kernel used for chat completion.
        /// </summary>
        protected virtual Kernel Kernel { get; set; } = default!;

        /// <summary>
        /// Streams a message to the agent using a <see cref="ChatMessageContent"/> object, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <param name="content">The user message content.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="onResponse">Callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The final <see cref="AgentMessage"/> response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="onResponse"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="SessionId"/> is not set.</exception>
        /// <remarks>
        /// This method adds the user message to history, streams the response, and saves both user and agent messages.
        /// </remarks>
        public override async Task<AgentMessage> StreamMessageAsync(
            ChatMessageContent content,
            Action<StreamingChatMessageContent> onResponse,
            PromptExecutionSettings? executionSettings = default,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (onResponse == null)
                {
                    Logger.LogError("StreamMessageAsync called with null onResponse callback.");
                    throw new ArgumentNullException(nameof(onResponse));
                }
                if (string.IsNullOrWhiteSpace(SessionId))
                {
                    Logger.LogError("StreamMessageAsync called without a valid SessionId.");
                    throw new InvalidOperationException("SessionId is not set. Please call StartSession() before streaming messages.");
                }

                Logger.LogDebug("Adding user message to history for streaming. Content: {Content}", content.Content);
                History.Add(content);
                Kernel = InitializeKernel();
                var service = Kernel.GetRequiredService<IChatCompletionService>();
                var sb = new StringBuilder();
                StreamingChatMessageContent last = default!;
                Logger.LogInformation("Starting streaming chat message contents.");
                await foreach (var r in service.GetStreamingChatMessageContentsAsync(History, executionSettings ?? DefaultExecutionSettings, Kernel, cancellationToken))
                {
                    try
                    {
                        Logger.LogDebug("Received streaming response chunk. Content: {ChunkContent}", r.Content);
                        onResponse(r);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error in onResponse callback during streaming.");
                    }
                    last = r;
                    sb.Append(r.Content);
                }
                Logger.LogInformation("Streaming complete. Assembling final chat message.");
                var chatMesage = new ChatMessageContent()
                {
                    Role = last.Role.Value,
                    Content = sb.ToString(),
                    InnerContent = last.InnerContent,
                    MimeType = content.MimeType,
                    ModelId = last.ModelId,
                    Metadata = last.Metadata,
                    Encoding = last.Encoding,
                };
                var agentResponse = AgentMessage.Create(SessionId, this, chatMesage);
                OnMessageCompleted(chatMesage, agentResponse);

                //try and get token count
                var usage = chatMesage.TryGetUsage();
                if(usage != null)
                {
                    agentResponse.InputTokens = usage.InputTokens;
                    agentResponse.OutputTokens = usage.OutputTokens;
                    agentResponse.TotalTokens = usage.TotalTokens;
                }

                Logger.LogDebug("Saving streamed user and agent messages.");
                await SaveMessageAsync(AgentMessage.Create(SessionId, this, content), agentResponse, cancellationToken);
                Logger.LogInformation("Streamed message saved successfully.");
                return agentResponse;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("StreamMessageAsync was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in StreamMessageAsync.");
                throw;
            }
        }


        /// <summary>
        /// Sends a message to the agent using a <see cref="ChatMessageContent"/> object, returning the agent's response.
        /// </summary>
        /// <param name="content">The user message content.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The <see cref="AgentMessage"/> response.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="SessionId"/> is not set.</exception>
        /// <remarks>
        /// This method adds the user message to history, sends the message, and saves both user and agent messages.
        /// </remarks>
        public override async Task<AgentMessage> SendMessageAsync(
              ChatMessageContent content,
              PromptExecutionSettings? executionSettings = default,
              CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SessionId))
                {
                    Logger.LogError("GetMessageAsync called without a valid SessionId.");
                    throw new InvalidOperationException("SessionId is not set. Please call StartSession() before streaming messages.");
                }
                Kernel = InitializeKernel();
                var service = Kernel.GetRequiredService<IChatCompletionService>();
                Logger.LogDebug("Adding user message to history for GetMessageAsync. Content: {Content}", content.Content);
                History.Add(content);

                OnBeforeMessageSent(content);

                Logger.LogInformation("Requesting chat message content from service.");
                var apiResponse = await service.GetChatMessageContentAsync(History, executionSettings, Kernel, cancellationToken);
                var agentResponse = AgentMessage.Create(SessionId, this, apiResponse);
                try
                {
                    Logger.LogDebug("Calling OnMessageCompleted.");
                    OnMessageCompleted(apiResponse, agentResponse);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error in OnMessageCompleted.");
                }
                Logger.LogDebug("Saving user and agent messages for GetMessageAsync.");

                //try and get token count
                var usage = apiResponse.TryGetUsage();
                if (usage != null)
                {
                    agentResponse.InputTokens = usage.InputTokens;
                    agentResponse.OutputTokens = usage.OutputTokens;
                    agentResponse.TotalTokens = usage.TotalTokens;
                }

                await SaveMessageAsync(AgentMessage.Create(SessionId, this, content), agentResponse, cancellationToken);
                Logger.LogInformation("GetMessageAsync completed and messages saved.");
                return agentResponse;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("GetMessageAsync was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in GetMessageAsync.");
                throw;
            }
        }
    }
}
