
namespace MarinApp.Agents.Data
{
    public interface IAgentHistoryService
    {
        Task<AgentMessage> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default);
        Task<List<AgentMessage>> GetMessagesByAgentAsync(string agentId, CancellationToken cancellationToken = default);
        Task<List<AgentMessage>> GetMessagesBySessionAsync(string sessionId, CancellationToken cancellationToken = default);
        Task SaveMessageAsync(AgentMessage message, CancellationToken cancellationToken = default);
    }
}