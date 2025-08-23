using Microsoft.EntityFrameworkCore;

namespace MarinApp.Agents.Data
{
    /// <summary>
    /// Provides an implementation of <see cref="IAgentHistoryService"/> for managing the persistent storage and retrieval
    /// of <see cref="AgentMessage"/> entities using an <see cref="AgentDataContext"/> (Entity Framework Core DbContext).
    /// <para>
    /// This service enables conversational agents to save, query, and delete chat history messages by session, agent, or message ID.
    /// It is designed to be used by agents for restoring previous conversations, persisting new messages, and managing message lifecycles.
    /// </para>
    /// <para>
    /// <b>Usage:</b> Register this service in the dependency injection container and inject it into agents or other services
    /// that require access to agent message history.
    /// </para>
    /// <para>
    /// <b>Exception Handling:</b> All methods provide robust exception handling, including specific handling for database update
    /// exceptions, operation cancellation, and unexpected errors. Exceptions are wrapped with descriptive messages for easier debugging.
    /// </para>
    /// </summary>
    public class AgentHistoryService
    {
        /// <summary>
        /// The Entity Framework Core data context used for accessing agent messages.
        /// </summary>
        private readonly AgentDataContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentHistoryService"/> class.
        /// </summary>
        /// <param name="context">The <see cref="AgentDataContext"/> used for database operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
        public AgentHistoryService(AgentDataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }


        /// <summary>
        /// Persists a collection of <see cref="AgentMessage"/> entities to the underlying storage in a single batch operation.
        /// </summary>
        /// <param name="messages">
        /// An <see cref="IEnumerable{T}"/> of <see cref="AgentMessage"/> objects to be saved.
        /// Each message should be fully populated and valid for persistence.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous batch save operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="messages"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the database update fails.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation is canceled.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown if an unexpected error occurs during the save operation.
        /// </exception>
        /// <remarks>
        /// This method adds all provided <see cref="AgentMessage"/> entities to the database context and saves them in a single transaction.
        /// It is optimized for batch inserts and provides robust exception handling for database and unexpected errors.
        /// </remarks>
        public async Task SaveMessagesAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            try
            {
                _context.AgentMessages.AddRange(messages);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to save agent messages to the database.", ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while saving agent messages.", ex);
            }
        }

        /// <summary>
        /// Persists a new or updated <see cref="AgentMessage"/> entity to the underlying storage.
        /// </summary>
        /// <param name="message">The <see cref="AgentMessage"/> to save.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the database update fails.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs.</exception>
        public async Task SaveMessageAsync(AgentMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            try
            {
                _context.AgentMessages.Add(message);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Log or handle DB update issues
                throw new InvalidOperationException("Failed to save the agent message to the database.", ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while saving the agent message.", ex);
            }
        }

        /// <summary>
        /// Retrieves all <see cref="AgentMessage"/> entities associated with a specific session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session whose messages to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of
        /// <see cref="AgentMessage"/> objects belonging to the specified session, ordered by creation time.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessionId"/> is null or whitespace.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs.</exception>
        public async Task<List<AgentMessage>> GetMessagesBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentNullException(nameof(sessionId));
            try
            {
                return await _context.AgentMessages
                                     .Where(m => m.SessionId == sessionId)
                                     .OrderBy(m => m.UtcCreatedAt)
                                     .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve messages for session '{sessionId}'.", ex);
            }
        }

        /// <summary>
        /// Retrieves all <see cref="AgentMessage"/> entities for a specific agent within a specific session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <param name="agentId">The unique identifier of the agent.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of
        /// <see cref="AgentMessage"/> objects for the specified agent and session, ordered by creation time.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessionId"/> is null or whitespace.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs.</exception>
        public async Task<List<AgentMessage>> GetMessagesBySessionAndAgentAsync(string sessionId, string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentNullException(nameof(sessionId));
            try
            {
                return await _context.AgentMessages
                                     .Where(m => m.SessionId == sessionId && m.AgentId == agentId)
                                     .OrderBy(m => m.UtcCreatedAt)
                                     .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve messages for session '{sessionId}' and agent '{agentId}'.", ex);
            }
        }

        /// <summary>
        /// Retrieves all <see cref="AgentMessage"/> entities associated with a specific agent.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent whose messages to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of
        /// <see cref="AgentMessage"/> objects sent or received by the specified agent, ordered by creation time.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="agentId"/> is null or whitespace.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs.</exception>
        public async Task<List<AgentMessage>> GetMessagesByAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentNullException(nameof(agentId));
            try
            {
                return await _context.AgentMessages
                                     .Where(m => m.AgentId == agentId)
                                     .OrderBy(m => m.UtcCreatedAt)
                                     .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve messages for agent '{agentId}'.", ex);
            }
        }

        /// <summary>
        /// Retrieves a single <see cref="AgentMessage"/> by its unique message identifier.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the
        /// <see cref="AgentMessage"/> with the specified identifier, or throws if not found.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="messageId"/> is null or whitespace.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the message is not found.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs.</exception>
        public async Task<AgentMessage> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentNullException(nameof(messageId));
            try
            {
                return await _context.AgentMessages
                                     .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
                                     ?? throw new KeyNotFoundException($"Message with Id '{messageId}' not found.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve message with Id '{messageId}'.", ex);
            }
        }

        /// <summary>
        /// Deletes all <see cref="AgentMessage"/> entities associated with a specific session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session whose messages to delete.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessionId"/> is null or whitespace.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs during deletion.</exception>
        public async Task DeleteMessagesBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));

            try
            {
                var messages = await _context.AgentMessages
                    .Where(m => m.SessionId == sessionId)
                    .ToListAsync(cancellationToken);

                await RemoveMessagesAsync(messages, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred while deleting messages for session '{sessionId}'.", ex);
            }
        }

        /// <summary>
        /// Deletes all <see cref="AgentMessage"/> entities associated with a specific agent.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent whose messages to delete.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="agentId"/> is null or whitespace.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs during deletion.</exception>
        public async Task DeleteMessagesByAgentIdAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            try
            {
                var messages = await _context.AgentMessages
                    .Where(m => m.AgentId == agentId)
                    .ToListAsync(cancellationToken);
                await RemoveMessagesAsync(messages, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred while deleting messages for agent '{agentId}'.", ex);
            }
        }

        /// <summary>
        /// Deletes all <see cref="AgentMessage"/> entities for a specific agent within a specific session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <param name="agentId">The unique identifier of the agent.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessionId"/> or <paramref name="agentId"/> is null or whitespace.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs during deletion.</exception>
        public async Task DeleteMessagesBySessionAndAgentIdAsync(string sessionId, string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            try
            {
                var messages = await _context.AgentMessages
                    .Where(m => m.SessionId == sessionId && m.AgentId == agentId)
                    .ToListAsync(cancellationToken);
                await RemoveMessagesAsync(messages, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred while deleting messages for session '{sessionId}' and agent '{agentId}'.", ex);
            }
        }

        /// <summary>
        /// Removes the specified list of <see cref="AgentMessage"/> entities from the database and saves changes.
        /// </summary>
        /// <param name="messages">The list of messages to remove.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the asynchronous remove operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="messages"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the database update fails.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs.</exception>
        /// <remarks>
        /// This method removes all provided messages from the database and saves changes.
        /// It provides robust exception handling for database and unexpected errors.
        /// </remarks>
        protected async Task RemoveMessagesAsync(List<AgentMessage> messages, CancellationToken cancellationToken = default)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            try
            {
                if (messages.Count > 0)
                {
                    _context.AgentMessages.RemoveRange(messages);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to remove agent messages from the database.", ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while removing agent messages.", ex);
            }
        }

    }
}
