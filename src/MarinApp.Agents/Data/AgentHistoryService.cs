using Microsoft.EntityFrameworkCore;

namespace MarinApp.Agents.Data
{
    /// <summary>
    /// Provides an implementation of <see cref="IAgentHistoryService"/> for managing agent message history in memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service stores <see cref="AgentMessage"/> instances in an in-memory list for demonstration and development purposes.
    /// In a production environment, replace this with a persistent storage implementation (e.g., database, distributed cache).
    /// </para>
    /// <para>
    /// <b>Thread Safety:</b> All operations are thread-safe using a private lock object.
    /// </para>
    /// <para>
    /// <b>Usage:</b> Register this service as a singleton or scoped dependency in your DI container.
    /// </para>
    /// </remarks>
    public class AgentHistoryService : IAgentHistoryService
    {
        /// <summary>
        /// The in-memory store for agent messages.
        /// </summary>
        private readonly List<AgentMessage> _messages = new();

        /// <summary>
        /// Lock object for thread-safe access to the message store.
        /// </summary>
        private readonly object _lock = new();

        /// <inheritdoc />
        public Task DeleteMessagesByAgentIdAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            lock (_lock)
            {
                _messages.RemoveAll(m => m.AgentId == agentId);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteMessagesBySessionAndAgentIdAsync(string sessionId, string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            lock (_lock)
            {
                _messages.RemoveAll(m => m.SessionId == sessionId && m.AgentId == agentId);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteMessagesBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));

            lock (_lock)
            {
                _messages.RemoveAll(m => m.SessionId == sessionId);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<AgentMessage> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                throw new ArgumentNullException(nameof(messageId));

            lock (_lock)
            {
                var message = _messages.FirstOrDefault(m => m.Id == messageId);
                return Task.FromResult(message);
            }
        }

        /// <inheritdoc />
        public Task<List<AgentMessage>> GetMessagesByAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            lock (_lock)
            {
                var result = _messages
                    .Where(m => m.AgentId == agentId)
                    .OrderBy(m => m.UtcCreatedAt)
                    .ToList();
                return Task.FromResult(result);
            }
        }

        /// <inheritdoc />
        public Task<List<AgentMessage>> GetMessagesBySessionAndAgentAsync(string sessionId, string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            lock (_lock)
            {
                var result = _messages
                    .Where(m => m.SessionId == sessionId && m.AgentId == agentId)
                    .OrderBy(m => m.UtcCreatedAt)
                    .ToList();
                return Task.FromResult(result);
            }
        }

        /// <inheritdoc />
        public Task<List<AgentMessage>> GetMessagesBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));

            lock (_lock)
            {
                var result = _messages
                    .Where(m => m.SessionId == sessionId)
                    .OrderBy(m => m.UtcCreatedAt)
                    .ToList();
                return Task.FromResult(result);
            }
        }

        /// <inheritdoc />
        public Task SaveMessageAsync(AgentMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            lock (_lock)
            {
                // Remove any existing message with the same Id (update scenario)
                _messages.RemoveAll(m => m.Id == message.Id);
                _messages.Add(message);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SaveMessagesAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            lock (_lock)
            {
                foreach (var message in messages)
                {
                    if (message == null) continue;
                    _messages.RemoveAll(m => m.Id == message.Id);
                    _messages.Add(message);
                }
            }
            return Task.CompletedTask;
        }
    }
}
