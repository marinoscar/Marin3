
namespace MarinApp.Agents.Data
{   
    /// <summary>
    /// Defines a contract for managing the persistent storage and retrieval of agent message history.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are responsible for providing asynchronous methods to
    /// save and retrieve <see cref="AgentMessage"/> entities, which represent messages exchanged
    /// by conversational agents within sessions. This service is typically used by agents to
    /// persist chat history, restore previous conversations, and query messages by agent or session.
    /// </remarks>
    public interface IAgentHistoryService
    {
        /// <summary>
        /// Retrieves a single <see cref="AgentMessage"/> by its unique message identifier.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the
        /// <see cref="AgentMessage"/> with the specified identifier, or <c>null</c> if not found.
        /// </returns>
        Task<AgentMessage> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all <see cref="AgentMessage"/> entities associated with a specific agent.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent whose messages to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of
        /// <see cref="AgentMessage"/> objects sent or received by the specified agent.
        /// </returns>
        Task<List<AgentMessage>> GetMessagesByAgentAsync(string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all <see cref="AgentMessage"/> entities associated with a specific session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session whose messages to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of
        /// <see cref="AgentMessage"/> objects belonging to the specified session.
        /// </returns>
        Task<List<AgentMessage>> GetMessagesBySessionAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all <see cref="AgentMessage"/> entities for a specific agent within a specific session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <param name="agentId">The unique identifier of the agent.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of
        /// <see cref="AgentMessage"/> objects for the specified agent and session.
        /// </returns>
        Task<List<AgentMessage>> GetMessagesBySessionAndAgentAsync(string sessionId, string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a new or updated <see cref="AgentMessage"/> entity to the underlying storage.
        /// </summary>
        /// <param name="message">The <see cref="AgentMessage"/> to save.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous save operation.
        /// </returns>
        Task SaveMessageAsync(AgentMessage message, CancellationToken cancellationToken = default);
    }
}