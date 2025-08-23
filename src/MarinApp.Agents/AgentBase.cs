using MarinApp.Agents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace MarinApp.Agents
{
    public class AgentBase
    {

        public AgentBase(Kernel kernel, IAgentHistoryService agentHistoryService,  ILoggerFactory loggerFactory)
        {
            Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            HistoryService = agentHistoryService ?? throw new ArgumentNullException(nameof(agentHistoryService));
            Logger = loggerFactory?.CreateLogger(this.GetType().Name) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;


        public virtual Kernel Kernel { get; protected set; } = default!;

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

        protected virtual async Task<AgentMessage> StreamMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default)
        {
            if(onResponse == null) throw new ArgumentNullException(nameof(onResponse));
            if(string.IsNullOrWhiteSpace(SessionId)) throw new InvalidOperationException("SessionId is not set. Please call StartSession() before streaming messages.");

            var history = new ChatHistory
            {
                content
            };

            var service = Kernel.GetRequiredService<IChatCompletionService>();
            var sb = new StringBuilder();
            StreamingChatMessageContent last = default!;
            await foreach (var r in service.GetStreamingChatMessageContentsAsync(history, executionSettings, Kernel, cancellationToken))
            {
                onResponse(r);
                last = r;
                sb.Append(r.Content);
            }
            var res = new AgentMessage() { 
                SessionId = SessionId,
                AgentId = this.Id,
                AgentName = this.Name,
                Role = Convert.ToString(last.Role) ?? throw new ArgumentNullException(nameof(last.Role)),
                Content = sb.ToString(),
                InnerContent = System.Text.Json.JsonSerializer.Serialize(last.InnerContent) ?? "{}",
                MimeType = content.MimeType ?? "text/markdown",
                ModelId = content.ModelId ?? throw new ArgumentNullException(nameof(content.ModelId)),
                Metadata = content.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(content.Metadata) : "{}"
            };
            OnStreamCompleted(last, res);
            await SaveMessageAsync(res, cancellationToken);
            return res;
        }

        protected virtual void OnStreamCompleted(StreamingChatMessageContent lastContent, AgentMessage agentMessage)
        {
            // Override in derived classes to handle stream completion events.
        }

        protected virtual async Task<AgentMessage> GetMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(SessionId)) throw new InvalidOperationException("SessionId is not set. Please call StartSession() before streaming messages.");
            var service = Kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory
            {
                content
            };
            var response = await service.GetChatMessageContentAsync(history, executionSettings, Kernel, cancellationToken); 
            var res = AgentMessage.Create(SessionId, this, response);
            OnMessageCompleted(response, res);
            await SaveMessageAsync(res, cancellationToken);
            return res;
        }

        protected virtual void OnMessageCompleted(ChatMessageContent messageContent, AgentMessage agentMessage)
        {
            // Override in derived classes to handle stream completion events.
        }

        protected virtual async Task SaveMessageAsync(AgentMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(SessionId)) throw new InvalidOperationException("SessionId is not set. Please call StartSession() before saving messages.");
            await HistoryService.SaveMessageAsync(message, cancellationToken);
        }



        public override string ToString()
        {
            return $"{Name} ({Id}) - {Description}";
        }

    }
}
