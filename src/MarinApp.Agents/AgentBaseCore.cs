using HandlebarsDotNet;
using MarinApp.Agents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MarinApp.Agents
{
    public abstract class AgentBaseCore : IAgent
    {


        public AgentBaseCore(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory)
        {
            HistoryService = agentHistoryService ?? throw new ArgumentNullException(nameof(agentHistoryService));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(this.GetType().Name) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }


        /// <inheritdoc />
        public string Id { get; set; } = default!;
        /// <inheritdoc />
        public string Name { get; set; } = default!;
        /// <inheritdoc />
        public string Description { get; set; } = default!;
        /// <inheritdoc />
        public virtual ChatHistory History { get; protected set; } = [];
        /// <inheritdoc />
        public virtual string SystemPrompt { get; protected set; } = default!;

        /// <summary>
        /// Gets the current session identifier.
        /// </summary>
        protected virtual string SessionId { get; set; } = default!;

        /// <inheritdoc />
        public event EventHandler<(ChatMessageContent MessageContent, AgentMessage AgentMessage)>? MessageCompleted;

        /// <summary>
        /// Gets the logger for this agent.
        /// </summary>
        protected virtual ILogger Logger { get; private set; } = default!;

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        protected virtual ILoggerFactory LoggerFactory { get; private set; }

        /// <summary>
        /// Gets the service for managing agent message history.
        /// </summary>
        protected virtual IAgentHistoryService HistoryService { get; set; }


        /// <summary>
        /// Starts a new session and resets the chat history.
        /// </summary>
        /// <returns>The new session identifier.</returns>
        /// <remarks>
        /// This method generates a new session ID, resets the chat history, and sets the session context.
        /// </remarks>
        public virtual string StartSession()
        {
            SessionId = Guid.NewGuid().ToString().Replace("-", "").ToUpperInvariant();
            SetSession(SessionId);
            return SessionId;
        }

        /// <summary>
        /// Sets the current session to the specified session ID and resets the chat history.
        /// </summary>
        /// <param name="sessionId">The session identifier to set.</param>
        /// <returns>The session identifier that was set.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessionId"/> is null or whitespace.</exception>
        public virtual string SetSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                Logger.LogError("SetSession called with null or whitespace sessionId.");
                throw new ArgumentNullException(nameof(sessionId));
            }
            History = new ChatHistory();
            SessionId = sessionId;
            Logger.LogInformation("Session set. SessionId: {SessionId}", SessionId);
            return SessionId;
        }

        /// <summary>
        /// Restores the chat history for the specified session from persistent storage.
        /// </summary>
        /// <param name="sessionId">The session identifier to restore.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessionId"/> is null or whitespace.</exception>
        /// <remarks>
        /// This method loads all messages for the given session and agent, and populates the <see cref="History"/> property.
        /// </remarks>
        public virtual async Task RestoreHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                Logger.LogError("RestoreSessionAsync called with null or whitespace sessionId.");
                throw new ArgumentNullException(nameof(sessionId));
            }
            var messages = await HistoryService.GetMessagesBySessionAndAgentAsync(sessionId, Id, cancellationToken);
            SetSession(sessionId);
            foreach (var m in messages)
            {
                try
                {
                    var content = new ChatMessageContent
                    {
                        Role = Enum.Parse<AuthorRole>(m.Role, true),
                        Content = m.Content,
                        MimeType = m.MimeType,
                        ModelId = m.ModelId,
                        Metadata = string.IsNullOrWhiteSpace(m.Metadata) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(m.Metadata) ?? new Dictionary<string, object>()
                    };
                    History.Add(content);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error deserializing message in RestoreSessionAsync. MessageId: {MessageId}", m.Id);
                }
            }
        }

        /// <summary>
        /// Sets the system prompt using a Handlebars template and data model.
        /// </summary>
        /// <typeparam name="T">The type of the data model.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to apply to the template.</param>
        /// <remarks>
        /// This method compiles the template and applies the data to generate the system prompt.
        /// </remarks>
        public virtual void SetSystemMessage<T>(string template, T data)
        {
            SetSystemMessage(ParseTemplate(template, data));
        }

        /// <summary>
        /// Sets the system prompt to the specified message.
        /// </summary>
        /// <param name="message">The system prompt message.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null or whitespace.</exception>
        public virtual void SetSystemMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Logger.LogError("SetSystemMessage called with null or whitespace message.");
                throw new ArgumentNullException(nameof(message));
            }
            Logger.LogDebug("SystemPrompt set to: {Message}", message);
            SystemPrompt = message;
        }

        /// <summary>
        /// Resets the chat history and adds the current system prompt as the first message.
        /// </summary>
        /// <remarks>
        /// If <see cref="SystemPrompt"/> is empty, a default prompt is used.
        /// </remarks>
        protected virtual void ResetHistory()
        {
            try
            {
                Logger.LogDebug("Resetting chat history.");
                History.Clear();
                var sysPrompt = SystemPrompt;
                if (string.IsNullOrEmpty(sysPrompt))
                {
                    Logger.LogDebug("SystemPrompt is empty, using default system prompt.");
                    sysPrompt = "You are a helpful assistant.";
                }
                History.AddSystemMessage(sysPrompt);
                Logger.LogInformation("Chat history reset. SystemPrompt: {SystemPrompt}", sysPrompt);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error resetting chat history in ResetHistory.");
                throw;
            }
        }

        /// <summary>
        /// Parses a Handlebars template with the specified data model.
        /// </summary>
        /// <typeparam name="T">The type of the data model.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to apply to the template.</param>
        /// <returns>The parsed template string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/> or <paramref name="data"/> is null or whitespace.</exception>
        /// <remarks>
        /// <b>Example:</b>
        /// <code>
        /// var result = agent.ParseTemplate("Hello, {{name}}!", new { name = "Alice" });
        /// // result: "Hello, Alice!"
        /// </code>
        /// </remarks>
        protected virtual string ParseTemplate<T>(string template, T data)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                Logger.LogError("ParseTemplate called with null or whitespace template.");
                throw new ArgumentNullException(nameof(template));
            }
            if (data == null)
            {
                Logger.LogError("ParseTemplate called with null data.");
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                Logger.LogDebug("Compiling Handlebars template in ParseTemplate.");
                var compiled = Handlebars.Compile(template);
                var result = compiled(data);
                Logger.LogDebug("Template parsed successfully. Result: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error compiling or applying Handlebars template in ParseTemplate.");
                throw;
            }
        }

        /// <inheritdoc />
        public abstract Task<AgentMessage> SendMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default);

        /// <inheritdoc />
        public abstract Task<AgentMessage> SendMessageAsync(string prompt, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default);

        /// <inheritdoc />
        public abstract Task<AgentMessage> SendMessageAsync<T>(string template, T data, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default);


        /// <inheritdoc />
        public abstract Task<AgentMessage> StreamMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);

        /// <inheritdoc />
        public abstract Task<AgentMessage> StreamMessageAsync(string prompt, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);

        /// <inheritdoc />
        public abstract Task<AgentMessage> StreamMessageAsync<T>(string template, T data, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
    }
}
