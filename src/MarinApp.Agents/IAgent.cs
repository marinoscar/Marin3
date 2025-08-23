using MarinApp.Agents.Data;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MarinApp.Agents
{
    public interface IAgent
    {
        string Description { get; set; }
        ChatHistory History { get; }
        string Id { get; set; }
        string Name { get; set; }
        string SystemPrompt { get; }

        event EventHandler<(ChatMessageContent MessageContent, AgentMessage AgentMessage)>? MessageCompleted;

        Task RestoreHistoryAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<AgentMessage> SendMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default);
        Task<AgentMessage> SendMessageAsync(string prompt, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default);
        Task<AgentMessage> SendMessageAsync<T>(string template, T data, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default);
        string SetSession(string sessionId);
        void SetSystemMessage(string message);
        void SetSystemMessage<T>(string template, T data);
        string StartSession();
        Task<AgentMessage> StreamMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
        Task<AgentMessage> StreamMessageAsync(string prompt, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
        Task<AgentMessage> StreamMessageAsync<T>(string template, T data, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
    }
}