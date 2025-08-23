using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Data
{
    /// <summary>
    /// Provides services for persisting and retrieving agent message history in the database.
    /// </summary>
    /// <remarks>
    /// This service is responsible for saving, querying, and retrieving <see cref="AgentMessage"/> entities
    /// from the underlying <see cref="AgentDataContext"/>. It supports filtering by session, agent, and message ID.
    /// </remarks>
    public class AgentHistoryService : IAgentHistoryService
    {
        private readonly AgentDataContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentHistoryService"/> class.
        /// </summary>
        /// <param name="context">The <see cref="AgentDataContext"/> used for data access.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
        public AgentHistoryService(AgentDataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Saves a single <see cref="AgentMessage"/> to the database asynchronously.
        /// </summary>
        /// <param name="message">The <see cref="AgentMessage"/> to save.</param>
        /// <param name="cancellationToken">A cancellation token for the async operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
        public async Task SaveMessageAsync(AgentMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _context.AgentMessages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves all messages for a given session, ordered by creation time.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="cancellationToken">A cancellation token for the async operation.</param>
        /// <returns>A list of <see cref="AgentMessage"/> objects for the session.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessionId"/> is null or whitespace.</exception>
        public async Task<List<AgentMessage>> GetMessagesBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentNullException(nameof(sessionId));
            return await _context.AgentMessages
                                 .Where(m => m.SessionId == sessionId)
                                 .OrderBy(m => m.UtcCreatedAt)
                                 .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves all messages for a given session and agent, ordered by creation time.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="agentId">The agent identifier.</param>
        /// <param name="cancellationToken">A cancellation token for the async operation.</param>
        /// <returns>A list of <see cref="AgentMessage"/> objects for the session and agent.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessionId"/> is null or whitespace.</exception>
        public async Task<List<AgentMessage>> GetMessagesBySessionAndAgentAsync(string sessionId, string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentNullException(nameof(sessionId));
            return await _context.AgentMessages
                                 .Where(m => m.SessionId == sessionId && m.AgentId == agentId)
                                 .OrderBy(m => m.UtcCreatedAt)
                                 .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves all messages for a given agent, ordered by creation time.
        /// </summary>
        /// <param name="agentId">The agent identifier.</param>
        /// <param name="cancellationToken">A cancellation token for the async operation.</param>
        /// <returns>A list of <see cref="AgentMessage"/> objects for the agent.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="agentId"/> is null or whitespace.</exception>
        public async Task<List<AgentMessage>> GetMessagesByAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentNullException(nameof(agentId));
            return await _context.AgentMessages
                                 .Where(m => m.AgentId == agentId)
                                 .OrderBy(m => m.UtcCreatedAt)
                                 .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves a single <see cref="AgentMessage"/> by its unique identifier.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="cancellationToken">A cancellation token for the async operation.</param>
        /// <returns>The <see cref="AgentMessage"/> with the specified ID.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="messageId"/> is null or whitespace.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if no message with the specified ID is found.</exception>
        public async Task<AgentMessage> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentNullException(nameof(messageId));
            return await _context.AgentMessages
                                 .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
                                 ?? throw new KeyNotFoundException($"Message with Id '{messageId}' not found.");
        }
    }
}
