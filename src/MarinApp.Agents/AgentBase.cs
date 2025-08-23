using HandlebarsDotNet;
using MarinApp.Agents.Data;
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
    /// <b>Usage:</b> Inherit from <see cref="AgentBase"/> to implement a custom agent. Override <see cref="InitializeKernel"/> to provide a configured <see cref="Kernel"/> instance.
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
    public abstract class AgentBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentBase"/> class.
        /// </summary>
        /// <param name="agentHistoryService">The service for persisting and retrieving agent message history.</param>
        /// <param name="loggerFactory">The logger factory for creating loggers.</param>
        /// <exception cref="InvalidOperationException">Thrown if the kernel cannot be initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown if required dependencies are null.</exception>
        public AgentBase(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory)
        {
            Kernel = InitializeKernel() ?? throw new InvalidOperationException("Failed to initalize Kernel");
            HistoryService = agentHistoryService ?? throw new ArgumentNullException(nameof(agentHistoryService));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(this.GetType().Name) ?? throw new ArgumentNullException(nameof(loggerFactory));
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
        /// Gets or sets the unique identifier for the agent.
        /// </summary>
        public string Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the display name of the agent.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets the description of the agent.
        /// </summary>
        public string Description { get; set; } = default!;

        /// <summary>
        /// Gets the chat history for the current session.
        /// </summary>
        public virtual ChatHistory History { get; protected set; } = [];

        /// <summary>
        /// Gets the current system prompt for the agent.
        /// </summary>
        public virtual string SystemPrompt { get; protected set; } = default!;

        /// <summary>
        /// Gets the semantic kernel used for chat completion.
        /// </summary>
        protected virtual Kernel Kernel { get; set; } = default!;

        /// <summary>
        /// Gets the logger for this agent.
        /// </summary>
        protected ILogger Logger { get; private set; } = default!;

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; private set; }

        /// <summary>
        /// Gets the service for managing agent message history.
        /// </summary>
        protected virtual IAgentHistoryService HistoryService { get; set; }

        /// <summary>
        /// Gets the current session identifier.
        /// </summary>
        protected virtual string SessionId { get; set; } = default!;

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
        /// Streams a message to the agent using a template and data, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <typeparam name="T">The type of the data model.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to apply to the template.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="onResponse">Callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The final <see cref="AgentMessage"/> response.</returns>
        /// <remarks>
        /// <b>Example:</b>
        /// <code>
        /// await agent.StreamMessageAsync("Hello, {{name}}!", new { name = "Alice" }, settings, chunk => Console.WriteLine(chunk.Content));
        /// </code>
        /// </remarks>
        public virtual async Task<AgentMessage> StreamMessageAsync<T>(
            string template,
            T data,
            PromptExecutionSettings executionSettings,
            Action<StreamingChatMessageContent> onResponse,
            CancellationToken cancellationToken = default)
        {
            return await StreamMessageAsync(ParseTemplate(template, data), executionSettings, onResponse, cancellationToken);
        }

        /// <summary>
        /// Streams a message to the agent using a prompt string, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <param name="prompt">The prompt string.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="onResponse">Callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The final <see cref="AgentMessage"/> response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="prompt"/> is null or whitespace.</exception>
        public virtual async Task<AgentMessage> StreamMessageAsync(
            string prompt,
            PromptExecutionSettings executionSettings,
            Action<StreamingChatMessageContent> onResponse,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Logger.LogError("StreamMessageAsync called with null or whitespace prompt.");
                throw new ArgumentNullException(nameof(prompt));
            }

            try
            {
                Logger.LogDebug("Creating ChatMessageContent for streaming. Prompt: {Prompt}", prompt);
                var content = new ChatMessageContent();
                content.Role = AuthorRole.User;
                content.Items.Add(new TextContent(prompt));

                return await StreamMessageAsync(content, executionSettings, onResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating ChatMessageContent in StreamMessageAsync(string).");
                throw;
            }
        }

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
        public virtual async Task<AgentMessage> StreamMessageAsync(
            ChatMessageContent content,
            PromptExecutionSettings executionSettings,
            Action<StreamingChatMessageContent> onResponse,
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
                var service = Kernel.GetRequiredService<IChatCompletionService>();
                var sb = new StringBuilder();
                StreamingChatMessageContent last = default!;
                Logger.LogInformation("Starting streaming chat message contents.");
                await foreach (var r in service.GetStreamingChatMessageContentsAsync(History, executionSettings, Kernel, cancellationToken))
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
        /// Sends a message to the agent using a template and data, returning the agent's response.
        /// </summary>
        /// <typeparam name="T">The type of the data model.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to apply to the template.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The <see cref="AgentMessage"/> response.</returns>
        /// <remarks>
        /// <b>Example:</b>
        /// <code>
        /// var response = await agent.SendMessageAsync("Hello, {{name}}!", new { name = "Alice" }, settings);
        /// </code>
        /// </remarks>
        public virtual async Task<AgentMessage> SendMessageAsync<T>(
            string template,
            T data,
            PromptExecutionSettings executionSettings,
            CancellationToken cancellationToken = default)
        {
            return await SendMessageAsync(ParseTemplate(template, data), executionSettings, cancellationToken);
        }

        /// <summary>
        /// Sends a message to the agent using a prompt string, returning the agent's response.
        /// </summary>
        /// <param name="prompt">The prompt string.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The <see cref="AgentMessage"/> response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="prompt"/> is null or whitespace.</exception>
        public virtual async Task<AgentMessage> SendMessageAsync(
            string prompt,
            PromptExecutionSettings executionSettings,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Logger.LogError("GetMessageAsync called with null or whitespace prompt.");
                throw new ArgumentNullException(nameof(prompt));
            }

            try
            {
                Logger.LogDebug("Creating ChatMessageContent for GetMessageAsync. Prompt: {Prompt}", prompt);
                var content = new ChatMessageContent();
                content.Role = AuthorRole.User;
                content.Items.Add(new TextContent(prompt));
                return await SendMessageAsync(content, executionSettings, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating ChatMessageContent in GetMessageAsync(string).");
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
        public virtual async Task<AgentMessage> SendMessageAsync(
              ChatMessageContent content,
              PromptExecutionSettings executionSettings,
              CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SessionId))
                {
                    Logger.LogError("GetMessageAsync called without a valid SessionId.");
                    throw new InvalidOperationException("SessionId is not set. Please call StartSession() before streaming messages.");
                }
                var service = Kernel.GetRequiredService<IChatCompletionService>();
                Logger.LogDebug("Adding user message to history for GetMessageAsync. Content: {Content}", content.Content);
                History.Add(content);

                Logger.LogDebug("Calling OnBeforeMessageSent.");
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

        /// <summary>
        /// Called when a message has been completed and processed by the agent.
        /// </summary>
        /// <param name="messageContent">The chat message content.</param>
        /// <param name="agentMessage">The agent message entity.</param>
        /// <remarks>
        /// Override this method in derived classes to handle message completion events.
        /// </remarks>
        protected virtual void OnMessageCompleted(ChatMessageContent messageContent, AgentMessage agentMessage)
        {
            Logger.LogDebug("OnMessageCompleted called. MessageContent: {Content}, AgentMessageId: {AgentMessageId}", messageContent?.Content, agentMessage?.Id);

            MessageCompleted?.Invoke(this, (messageContent, agentMessage));

            // Override in derived classes to handle stream completion events.
        }

        /// <summary>
        /// Called before a message is sent to the agent.
        /// </summary>
        /// <param name="messageContent">The chat message content.</param>
        /// <remarks>
        /// Override this method in derived classes to handle events before sending a message.
        /// </remarks>
        public virtual void OnBeforeMessageSent(ChatMessageContent messageContent)
        {
            Logger.LogDebug("OnBeforeMessageSent called. MessageContent: {Content}", messageContent?.Content);
            // Override in derived classes to handle events before sending a message.
        }

        /// <summary>
        /// Called after user and agent messages have been saved.
        /// </summary>
        /// <param name="userMessage">The user message entity.</param>
        /// <param name="agentResponse">The agent response entity.</param>
        /// <remarks>
        /// Override this method in derived classes to handle events after saving messages.
        /// </remarks>
        protected virtual void OnMessageSaved(AgentMessage userMessage, AgentMessage agentResponse)
        {
            Logger.LogDebug("OnMessageSaved called. UserMessageId: {UserMessageId}, AgentResponseId: {AgentResponseId}", userMessage?.Id, agentResponse?.Id);
            // Override in derived classes to handle events after saving messages.
        }

        /// <summary>
        /// Saves the user and agent messages to persistent storage.
        /// </summary>
        /// <param name="userMessage">The user message entity.</param>
        /// <param name="agentResponse">The agent response entity.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="userMessage"/> or <paramref name="agentResponse"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="SessionId"/> is not set.</exception>
        /// <remarks>
        /// This method saves both the user and agent messages and then calls <see cref="OnMessageSaved"/>.
        /// </remarks>
        protected virtual async Task SaveMessageAsync(AgentMessage userMessage, AgentMessage agentResponse, CancellationToken cancellationToken = default)
        {
            try
            {
                if (agentResponse == null)
                {
                    Logger.LogError("SaveMessageAsync called with null agentResponse.");
                    throw new ArgumentNullException(nameof(agentResponse));
                }
                if (userMessage == null)
                {
                    Logger.LogError("SaveMessageAsync called with null userMessage.");
                    throw new ArgumentNullException(nameof(userMessage));
                }

                if (string.IsNullOrWhiteSpace(SessionId))
                {
                    Logger.LogError("SaveMessageAsync called without a valid SessionId.");
                    throw new InvalidOperationException("SessionId is not set. Please call StartSession() before saving messages.");
                }

                Logger.LogDebug("Saving user message. Id: {UserMessageId}", userMessage.Id);
                await HistoryService.SaveMessageAsync(userMessage, cancellationToken);
                Logger.LogDebug("Saving agent response message. Id: {AgentResponseId}", agentResponse.Id);
                await HistoryService.SaveMessageAsync(agentResponse, cancellationToken);

                Logger.LogDebug("Calling OnMessageSaved after saving messages.");
                OnMessageSaved(userMessage, agentResponse);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("SaveMessageAsync was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in SaveMessageAsync.");
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

        /// <summary>
        /// Sets the agent's identifying details.
        /// </summary>
        /// <param name="id">The unique identifier for the agent.</param>
        /// <param name="name">The display name of the agent. If null or whitespace, <paramref name="id"/> is used.</param>
        /// <param name="description">The description of the agent.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="id"/> is null or whitespace.</exception>
        protected virtual void SetAgentDetails(string id, string name = default!, string description = default!)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Logger.LogError("SetAgentDetails called with null or whitespace id.");
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? id : name;
            Description = description ?? string.Empty;
        }

        /// <summary>
        /// Returns a string representation of the agent, including its name, ID, and description.
        /// </summary>
        /// <returns>A string describing the agent.</returns>
        public override string ToString()
        {
            Logger.LogDebug("ToString called for AgentBase.");
            return $"{Name} ({Id}) - {Description}";
        }
    }
}
