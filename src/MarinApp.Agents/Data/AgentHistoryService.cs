using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Data
{
    public class AgentHistoryService : IAgentHistoryService
    {
        private readonly AgentDataContext _context;

        public AgentHistoryService(AgentDataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

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
        /// Removes the specified list of <see cref="AgentMessage"/> entities from the database.
        /// </summary>
        /// <param name="messages">The list of messages to remove.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="messages"/> is null.</exception>
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
