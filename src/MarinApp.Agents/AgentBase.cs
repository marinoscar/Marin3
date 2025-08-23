using HandlebarsDotNet;
using MarinApp.Agents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace MarinApp.Agents
{
    /// <summary>
    /// Provides a base class for conversational agents, encapsulating session management, message streaming, templating, and history persistence.
    /// </summary>
    public abstract class AgentBase : IAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentBase"/> class with the specified kernel, history service, and logger factory.
        /// </summary>
        /// <param name="agentHistoryService">The service responsible for persisting and retrieving agent message history.</param>
        /// <param name="loggerFactory">The logger factory used to create a logger for this agent.</param>
        /// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
        public AgentBase(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory)
        {
            Kernel = InitializeKernel() ?? throw new InvalidOperationException("Failed to initalize Kernel");
            HistoryService = agentHistoryService ?? throw new ArgumentNullException(nameof(agentHistoryService));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(this.GetType().Name) ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger.LogDebug("AgentBase constructed with Kernel: {KernelType}, HistoryService: {HistoryServiceType}", kernel.GetType().Name, agentHistoryService.GetType().Name);
        }

        /// <summary>
        /// When implemented in a derived class, initializes and returns a new instance of the <see cref="Kernel"/> used by the agent.
        /// <para>
        /// The <see cref="Kernel"/> is responsible for providing semantic services such as chat completion, prompt execution, and service resolution.
        /// This method should be overridden in concrete agent implementations to configure and return a properly initialized <see cref="Kernel"/> instance
        /// with all required plugins, models, and services registered.
        /// </para>
        /// <para>
        /// <b>Example override:</b>
        /// <code>
        /// protected override Kernel InitializeKernel()
        /// {
        ///     var builder = new KernelBuilder();
        ///     builder.AddChatCompletionService(...);
        ///     builder.AddPlugin(...);
        ///     return builder.Build();
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// <b>Exceptions:</b>
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// If the method returns <c>null</c>, the <see cref="AgentBase"/> constructor will throw an <see cref="ArgumentNullException"/>.
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <returns>
        /// A fully initialized <see cref="Kernel"/> instance to be used by the agent for semantic operations.
        /// </returns>
        protected abstract Kernel InitializeKernel();

        /// <summary>
        /// Occurs when a message has been completed and processed by the agent.
        /// </summary>
        public event EventHandler<(ChatMessageContent MessageContent, AgentMessage AgentMessage)>? MessageCompleted;

        /// <summary>
        /// Gets or sets the unique identifier for this agent.
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
        /// Gets the current chat history for the agent session.
        /// </summary>
        public virtual ChatHistory History { get; protected set; } = new ChatHistory();

        /// <summary>
        /// Gets the current system prompt used by the agent.
        /// </summary>
        public virtual string SystemPrompt { get; protected set; } = default!;

        /// <summary>
        /// Gets the semantic kernel used for chat completion and service resolution.
        /// </summary>
        protected virtual Kernel Kernel { get; set; } = default!;

        /// <summary>
        /// Gets the logger instance for this agent.
        /// </summary>
        protected ILogger Logger { get; private set; } = default!;

        /// <summary>
        /// Gets the logger factory used to create loggers for this agent.
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; private set; }

        /// <summary>
        /// Gets the history service used for persisting and retrieving agent messages.
        /// </summary>
        protected virtual IAgentHistoryService HistoryService { get; set; }

        /// <summary>
        /// Gets the current session identifier for the agent.
        /// </summary>
        protected virtual string SessionId { get; set; } = default!;

        /// <summary>
        /// Starts a new session for the agent, generating a new session identifier and resetting the session state.
        /// </summary>
        /// <returns>The new session identifier.</returns>
        public virtual string StartSession()
        {
            SessionId = Guid.NewGuid().ToString().Replace("-", "").ToUpperInvariant();
            SetSession(SessionId);
            return SessionId;
        }

        /// <summary>
        /// Sets the current session identifier and resets the chat history for the session.
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
        /// Sets the system message for the agent using a Handlebars template and data model.
        /// </summary>
        /// <typeparam name="T">The type of the data model used for template binding.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to bind to the template.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/> or <paramref name="data"/> is null or whitespace.</exception>
        public virtual void SetSystemMessage<T>(string template, T data)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                Logger.LogError("SetSystemMessage<T> called with null or whitespace template.");
                throw new ArgumentNullException(nameof(template));
            }
            if (data == null)
            {
                Logger.LogError("SetSystemMessage<T> called with null data.");
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                Logger.LogDebug("Compiling Handlebars template in SetSystemMessage<T>.");
                var t = Handlebars.Compile(template);
                var result = t(data);
                Logger.LogDebug("System message set using template. Result: {Result}", result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error compiling or applying Handlebars template in SetSystemMessage<T>.");
                throw;
            }
        }

        /// <summary>
        /// Sets the system message for the agent to a static message string.
        /// </summary>
        /// <param name="message">The system message to set.</param>
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
        /// Resets the chat history for the current session, adding the system prompt as the first message.
        /// </summary>
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
        /// Streams a message to the agent using a Handlebars template and data model, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <typeparam name="T">The type of the data model used for template binding.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to bind to the template.</param>
        /// <param name="executionSettings">The prompt execution settings for the chat completion service.</param>
        /// <param name="onResponse">A callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The final <see cref="AgentMessage"/> generated by the agent.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/>, <paramref name="data"/>, or <paramref name="onResponse"/> is null or whitespace.</exception>
        public virtual async Task<AgentMessage> StreamMessageAsync<T>(
            string template,
            T data,
            PromptExecutionSettings executionSettings,
            Action<StreamingChatMessageContent> onResponse,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                Logger.LogError("StreamMessageAsync<T> called with null or whitespace template.");
                throw new ArgumentNullException(nameof(template));
            }
            if (data == null)
            {
                Logger.LogError("StreamMessageAsync<T> called with null data.");
                throw new ArgumentNullException(nameof(data));
            }
            try
            {
                Logger.LogDebug("Compiling Handlebars template in StreamMessageAsync<T>.");
                var t = Handlebars.Compile(template);
                string result = t(data);
                Logger.LogDebug("Streaming message with compiled template result: {Result}", result);
                return await StreamMessageAsync(result, executionSettings, onResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error compiling or applying Handlebars template in StreamMessageAsync<T>.");
                throw;
            }
        }

        /// <summary>
        /// Streams a message to the agent using a static prompt string, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <param name="prompt">The prompt string to send to the agent.</param>
        /// <param name="executionSettings">The prompt execution settings for the chat completion service.</param>
        /// <param name="onResponse">A callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The final <see cref="AgentMessage"/> generated by the agent.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="prompt"/> or <paramref name="onResponse"/> is null or whitespace.</exception>
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
        /// <param name="content">The chat message content to send to the agent.</param>
        /// <param name="executionSettings">The prompt execution settings for the chat completion service.</param>
        /// <param name="onResponse">A callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The final <see cref="AgentMessage"/> generated by the agent.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="onResponse"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the session ID is not set.</exception>
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
        /// Gets a message from the agent using a Handlebars template and data model, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <typeparam name="T">The type of the data model used for template binding.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to bind to the template.</param>
        /// <param name="executionSettings">The prompt execution settings for the chat completion service.</param>
        /// <param name="onResponse">A callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The final <see cref="AgentMessage"/> generated by the agent.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/>, <paramref name="data"/>, or <paramref name="onResponse"/> is null or whitespace.</exception>
        public virtual async Task<AgentMessage> GetMessageAsync<T>(
            string template,
            T data,
            PromptExecutionSettings executionSettings,
            Action<StreamingChatMessageContent> onResponse,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                Logger.LogError("GetMessageAsync<T> called with null or whitespace template.");
                throw new ArgumentNullException(nameof(template));
            }
            if (data == null)
            {
                Logger.LogError("GetMessageAsync<T> called with null data.");
                throw new ArgumentNullException(nameof(data));
            }
            try
            {
                Logger.LogDebug("Compiling Handlebars template in GetMessageAsync<T>.");
                var t = Handlebars.Compile(template);
                string result = t(data);
                Logger.LogDebug("Getting message with compiled template result: {Result}", result);

                return await GetMessageAsync(result, executionSettings, onResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error compiling or applying Handlebars template in GetMessageAsync<T>.");
                throw;
            }
        }

        /// <summary>
        /// Gets a message from the agent using a static prompt string, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <param name="prompt">The prompt string to send to the agent.</param>
        /// <param name="executionSettings">The prompt execution settings for the chat completion service.</param>
        /// <param name="onResponse">A callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The final <see cref="AgentMessage"/> generated by the agent.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="prompt"/> or <paramref name="onResponse"/> is null or whitespace.</exception>
        public virtual async Task<AgentMessage> GetMessageAsync(
            string prompt,
            PromptExecutionSettings executionSettings,
            Action<StreamingChatMessageContent> onResponse,
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
                return await GetMessageAsync(content, executionSettings, onResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating ChatMessageContent in GetMessageAsync(string).");
                throw;
            }
        }

        /// <summary>
        /// Gets a message from the agent using a <see cref="ChatMessageContent"/> object, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <param name="content">The chat message content to send to the agent.</param>
        /// <param name="executionSettings">The prompt execution settings for the chat completion service.</param>
        /// <param name="onResponse">A callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The final <see cref="AgentMessage"/> generated by the agent.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the session ID is not set.</exception>
        public virtual async Task<AgentMessage> GetMessageAsync(
            ChatMessageContent content,
            PromptExecutionSettings executionSettings,
            Action<StreamingChatMessageContent> onResponse,
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
        /// Can be overridden in derived classes to handle stream completion events.
        /// </summary>
        /// <param name="messageContent">The chat message content that was completed.</param>
        /// <param name="agentMessage">The <see cref="AgentMessage"/> generated by the agent.</param>
        protected virtual void OnMessageCompleted(ChatMessageContent messageContent, AgentMessage agentMessage)
        {
            Logger.LogDebug("OnMessageCompleted called. MessageContent: {Content}, AgentMessageId: {AgentMessageId}", messageContent?.Content, agentMessage?.Id);

            MessageCompleted?.Invoke(this, (messageContent, agentMessage));

            // Override in derived classes to handle stream completion events.
        }

        /// <summary>
        /// Called before a message is sent to the agent.
        /// Can be overridden in derived classes to handle pre-send events.
        /// </summary>
        /// <param name="messageContent">The chat message content to be sent.</param>
        public virtual void OnBeforeMessageSent(ChatMessageContent messageContent)
        {
            Logger.LogDebug("OnBeforeMessageSent called. MessageContent: {Content}", messageContent?.Content);
            // Override in derived classes to handle events before sending a message.
        }

        /// <summary>
        /// Called after user and agent messages have been saved to the history service.
        /// Can be overridden in derived classes to handle post-save events.
        /// </summary>
        /// <param name="userMessage">The user message that was saved.</param>
        /// <param name="agentResponse">The agent response message that was saved.</param>
        protected virtual void OnMessageSaved(AgentMessage userMessage, AgentMessage agentResponse)
        {
            Logger.LogDebug("OnMessageSaved called. UserMessageId: {UserMessageId}, AgentResponseId: {AgentResponseId}", userMessage?.Id, agentResponse?.Id);
            // Override in derived classes to handle events after saving messages.
        }

        /// <summary>
        /// Saves the user and agent response messages to the history service.
        /// </summary>
        /// <param name="userMessage">The user message to save.</param>
        /// <param name="agentResponse">The agent response message to save.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="userMessage"/> or <paramref name="agentResponse"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the session ID is not set.</exception>
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
