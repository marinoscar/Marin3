using MarinApp.Agents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MarinApp.Agents
{
    public class AgentBase
    {

        public AgentBase(Kernel kernel, ILoggerFactory loggerFactory)
        {
            Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            Logger = loggerFactory?.CreateLogger(this.GetType().Name) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;


        public virtual Kernel Kernel { get; protected set; } = default!;

        protected ILogger Logger { get; private set; } = default!;

        protected virtual async Task<AgentMessage> StreamMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default)
        {
            if(onResponse == null) throw new ArgumentNullException(nameof(onResponse));

            var history = new ChatHistory
            {
                content
            };

            var service = Kernel.GetRequiredService<IChatCompletionService>();

            var res = new ChatMessageContent();
            await foreach (var r in service.GetStreamingChatMessageContentsAsync(history, executionSettings, Kernel, cancellationToken))
            {
                onResponse(r);
            }
        }

        protected virtual async Task GetMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default)
        {
            var service = Kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory
            {
                content
            };
            var response = await service.GetChatMessageContentAsync(history, executionSettings, Kernel, cancellationToken); 
        }



        public override string ToString()
        {
            return $"{Name} ({Id}) - {Description}";
        }

    }
}
