
namespace MarinApp.Agents.Data
{           
    /// <summary>
    /// Defines a contract for managing the persistent storage and retrieval of agent message history.
    /// Provides methods for saving, retrieving, and deleting messages by various criteria such as agent, session, or message ID.
    /// </summary>
    public interface IAgentHistoryService
    {
        /// <summary>
        /// Deletes all messages associated with the specified agent identifier.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent whose messages should be deleted.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task DeleteMessagesByAgentIdAsync(string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all messages associated with the specified session and agent identifiers.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <param name="agentId">The unique identifier of the agent.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task DeleteMessagesBySessionAndAgentIdAsync(string sessionId, string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all messages associated with the specified session identifier, regardless of agent.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session whose messages should be deleted.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task DeleteMessagesBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a single agent message by its unique message identifier.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous retrieval operation. The task result contains the <see cref="AgentMessage"/> if found; otherwise, <c>null</c>.
        /// </returns>
        Task<AgentMessage> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all messages associated with the specified agent identifier, across all sessions.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent whose messages should be retrieved.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous retrieval operation. The task result contains a list of <see cref="AgentMessage"/> objects.
        /// </returns>
        Task<List<AgentMessage>> GetMessagesByAgentAsync(string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all messages associated with the specified session and agent identifiers.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <param name="agentId">The unique identifier of the agent.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous retrieval operation. The task result contains a list of <see cref="AgentMessage"/> objects.
        /// </returns>
        Task<List<AgentMessage>> GetMessagesBySessionAndAgentAsync(string sessionId, string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all messages associated with the specified session identifier, regardless of agent.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session whose messages should be retrieved.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous retrieval operation. The task result contains a list of <see cref="AgentMessage"/> objects.
        /// </returns>
        Task<List<AgentMessage>> GetMessagesBySessionAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a single agent message to the underlying storage.
        /// </summary>
        /// <param name="message">The <see cref="AgentMessage"/> instance to save.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        Task SaveMessageAsync(AgentMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists multiple agent messages to the underlying storage in a batch operation.
        /// </summary>
        /// <param name="messages">An enumerable collection of <see cref="AgentMessage"/> instances to save.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous batch save operation.</returns>
        Task SaveMessagesAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default);
    }
}