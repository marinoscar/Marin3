using MarinApp.Agents.Data;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MarinApp.Agents
{
    public interface IAgentBase
    {
        string Description { get; set; }
        ChatHistory History { get; }
        string Id { get; set; }
        string Name { get; set; }
        string SystemPrompt { get; }

        event EventHandler<(ChatMessageContent MessageContent, AgentMessage AgentMessage)>? MessageCompleted;

        Task<AgentMessage> GetMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
        Task<AgentMessage> GetMessageAsync(string prompt, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
        Task<AgentMessage> GetMessageAsync<T>(string template, T data, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
        void OnBeforeMessageSent(ChatMessageContent messageContent);
        string SetSession(string sessionId);
        void SetSystemMessage(string message);
        void SetSystemMessage<T>(string template, T data);
        string StartSession();
        Task<AgentMessage> StreamMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
        Task<AgentMessage> StreamMessageAsync(string prompt, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
        Task<AgentMessage> StreamMessageAsync<T>(string template, T data, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
    }
}