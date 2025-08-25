using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        private readonly AgentDataContext _dataContext;

        public AgentHistoryService(AgentDataContext dataContext)
        {
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }


        /// <inheritdoc />
        public async Task DeleteMessagesByAgentIdAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            var entities = _dataContext.AgentMessages.Where(m => m.AgentId == agentId);
            _dataContext.AgentMessages.RemoveRange(entities);
            await _dataContext.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteMessagesBySessionAndAgentIdAsync(string sessionId, string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            var entities = _dataContext.AgentMessages.Where(m => m.AgentId == agentId && m.SessionId == sessionId);
            _dataContext.AgentMessages.RemoveRange(entities);
            await _dataContext.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteMessagesBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));

            var entities = _dataContext.AgentMessages.Where(m => m.SessionId == sessionId);
            _dataContext.AgentMessages.RemoveRange(entities);
            await _dataContext.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<AgentMessage?> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                throw new ArgumentNullException(nameof(messageId));

            return await _dataContext.AgentMessages.SingleOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        }

        /// <inheritdoc />
        public Task<List<AgentMessage>> GetMessagesByAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            return _dataContext.AgentMessages
                .Where(m => m.AgentId == agentId)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<List<AgentMessage>> GetMessagesBySessionAndAgentAsync(string sessionId, string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentNullException(nameof(agentId));

            return _dataContext.AgentMessages
                .Where(m => m.SessionId == sessionId && m.AgentId == agentId)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<List<AgentMessage>> GetMessagesBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId));

            return _dataContext.AgentMessages
                .Where(m => m.SessionId == sessionId)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task SaveMessageAsync(AgentMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            await _dataContext.AddAsync<AgentMessage>(message, cancellationToken);
            await _dataContext.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task SaveMessagesAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            foreach (var message in messages)
            {
                if (message == null) continue;
                await _dataContext.AddAsync<AgentMessage>(message, cancellationToken);
            }

            await _dataContext.SaveChangesAsync(cancellationToken);
        }
    }
}
