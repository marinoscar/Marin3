using HandlebarsDotNet;
using MarinApp.Agents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace MarinApp.Agents
{
    public class AgentBase
    {
        public AgentBase(Kernel kernel, IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory)
        {
            Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            HistoryService = agentHistoryService ?? throw new ArgumentNullException(nameof(agentHistoryService));
            Logger = loggerFactory?.CreateLogger(this.GetType().Name) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;

        protected virtual string SystemPrompt { get; set; } = default!;
        protected virtual Kernel Kernel { get; set; } = default!;
        protected ILogger Logger { get; private set; } = default!;
        protected virtual IAgentHistoryService HistoryService { get; set; }
        protected virtual string SessionId { get; set; } = default!;
        protected virtual ChatHistory History { get; set; } = new ChatHistory();

        public virtual string StartSession()
        {
            SessionId = Guid.NewGuid().ToString().Replace("-", "").ToUpperInvariant();
            History = new ChatHistory();
            return SessionId;
        }

        public virtual void SetSystemMessage<T>(string template, T data)
        {
            if (string.IsNullOrWhiteSpace(template)) throw new ArgumentNullException(nameof(template));
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                var t = Handlebars.Compile(template);
                var result = t(data);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error compiling or applying Handlebars template in SetSystemMessage<T>.");
                throw;
            }
        }

        public virtual void SetSystemMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));
            SystemPrompt = message;
        }

        protected virtual void ResetHistory()
        {
            try
            {
                History.Clear();
                var sysPrompt = SystemPrompt;
                if (string.IsNullOrEmpty(sysPrompt))
                    sysPrompt = "You are a helpful assistant.";
                History.AddSystemMessage(sysPrompt);
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
            if (string.IsNullOrWhiteSpace(template)) throw new ArgumentNullException(nameof(template));
            if (data == null) throw new ArgumentNullException(nameof(data));
            try
            {
                var t = Handlebars.Compile(template);
                string result = t(data);
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
            if (string.IsNullOrWhiteSpace(prompt)) throw new ArgumentNullException(nameof(prompt));

            try
            {
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
                if (onResponse == null) throw new ArgumentNullException(nameof(onResponse));
                if (string.IsNullOrWhiteSpace(SessionId)) throw new InvalidOperationException("SessionId is not set. Please call StartSession() before streaming messages.");

                History.Add(content);
                var service = Kernel.GetRequiredService<IChatCompletionService>();
                var sb = new StringBuilder();
                StreamingChatMessageContent last = default!;
                await foreach (var r in service.GetStreamingChatMessageContentsAsync(History, executionSettings, Kernel, cancellationToken))
                {
                    try
                    {
                        onResponse(r);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error in onResponse callback during streaming.");
                        // Optionally rethrow or continue
                    }
                    last = r;
                    sb.Append(r.Content);
                }
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

                await SaveMessageAsync(AgentMessage.Create(SessionId, this, content), agentResponse, cancellationToken);
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
            if (string.IsNullOrWhiteSpace(template)) throw new ArgumentNullException(nameof(template));
            if (data == null) throw new ArgumentNullException(nameof(data));
            try
            {
                var t = Handlebars.Compile(template);
                string result = t(data);

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
            if (string.IsNullOrWhiteSpace(prompt)) throw new ArgumentNullException(nameof(prompt));

            try
            {
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
            Action<StreamingChatMessageContent> onResponse,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SessionId)) throw new InvalidOperationException("SessionId is not set. Please call StartSession() before streaming messages.");
                var service = Kernel.GetRequiredService<IChatCompletionService>();
                History.Add(content);

                OnBeforeMessageSent(content);

                var apiResponse = await service.GetChatMessageContentAsync(History, executionSettings, Kernel, cancellationToken);
                var agentResponse = AgentMessage.Create(SessionId, this, apiResponse);
                try
                {
                    OnMessageCompleted(apiResponse, agentResponse);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error in OnMessageCompleted.");
                }
                await SaveMessageAsync(AgentMessage.Create(SessionId, this, content), agentResponse, cancellationToken);
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
            // Override in derived classes to handle stream completion events.
        }

        public virtual void OnBeforeMessageSent(ChatMessageContent messageContent)
        {
            // Override in derived classes to handle events before sending a message.
        }

        protected virtual void OnMessageSaved(AgentMessage userMessage, AgentMessage agentResponse)
        {
            // Override in derived classes to handle events after saving messages.
        }

        protected virtual async Task SaveMessageAsync(AgentMessage userMessage, AgentMessage agentResponse, CancellationToken cancellationToken = default)
        {
            try
            {
                if (agentResponse == null) throw new ArgumentNullException(nameof(agentResponse));
                if (userMessage == null) throw new ArgumentNullException(nameof(userMessage));

                if (string.IsNullOrWhiteSpace(SessionId)) throw new InvalidOperationException("SessionId is not set. Please call StartSession() before saving messages.");

                await HistoryService.SaveMessageAsync(userMessage, cancellationToken);
                await HistoryService.SaveMessageAsync(agentResponse, cancellationToken);

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
            return $"{Name} ({Id}) - {Description}";
        }
    }
}
