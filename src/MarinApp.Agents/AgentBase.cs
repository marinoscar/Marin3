using HandlebarsDotNet;
using MarinApp.Agents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace MarinApp.Agents
{
    public abstract class AgentBase
    {
        
        public AgentBase(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory)
        {
            Kernel = InitializeKernel() ?? throw new InvalidOperationException("Failed to initalize Kernel");
            HistoryService = agentHistoryService ?? throw new ArgumentNullException(nameof(agentHistoryService));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(this.GetType().Name) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        protected abstract Kernel InitializeKernel();

        public event EventHandler<(ChatMessageContent MessageContent, AgentMessage AgentMessage)>? MessageCompleted;

        public string Id { get; set; } = default!;

        public string Name { get; set; } = default!;

        public string Description { get; set; } = default!;

        public virtual ChatHistory History { get; protected set; } = [];

        public virtual string SystemPrompt { get; protected set; } = default!;

        protected virtual Kernel Kernel { get; set; } = default!;

        protected ILogger Logger { get; private set; } = default!;

        protected ILoggerFactory LoggerFactory { get; private set; }

        protected virtual IAgentHistoryService HistoryService { get; set; }

        protected virtual string SessionId { get; set; } = default!;

        public virtual string StartSession()
        {
            SessionId = Guid.NewGuid().ToString().Replace("-", "").ToUpperInvariant();
            SetSession(SessionId);
            return SessionId;
        }

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

        public virtual async Task<AgentMessage> GetMessageAsync(
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

        protected virtual void OnMessageCompleted(ChatMessageContent messageContent, AgentMessage agentMessage)
        {
            Logger.LogDebug("OnMessageCompleted called. MessageContent: {Content}, AgentMessageId: {AgentMessageId}", messageContent?.Content, agentMessage?.Id);

            MessageCompleted?.Invoke(this, (messageContent, agentMessage));

            // Override in derived classes to handle stream completion events.
        }

        public virtual void OnBeforeMessageSent(ChatMessageContent messageContent)
        {
            Logger.LogDebug("OnBeforeMessageSent called. MessageContent: {Content}", messageContent?.Content);
            // Override in derived classes to handle events before sending a message.
        }

        protected virtual void OnMessageSaved(AgentMessage userMessage, AgentMessage agentResponse)
        {
            Logger.LogDebug("OnMessageSaved called. UserMessageId: {UserMessageId}, AgentResponseId: {AgentResponseId}", userMessage?.Id, agentResponse?.Id);
            // Override in derived classes to handle events after saving messages.
        }

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

        public override string ToString()
        {
            Logger.LogDebug("ToString called for AgentBase.");
            return $"{Name} ({Id}) - {Description}";
        }
    }
}
