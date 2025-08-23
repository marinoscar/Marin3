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
            _context.AgentMessages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<List<AgentMessage>> GetMessagesBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentNullException(nameof(sessionId));
            return await _context.AgentMessages
                                 .Where(m => m.SessionId == sessionId)
                                 .OrderBy(m => m.UtcCreatedAt)
                                 .ToListAsync(cancellationToken);
        }
        public async Task<List<AgentMessage>> GetMessagesByAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentNullException(nameof(agentId));
            return await _context.AgentMessages
                                 .Where(m => m.AgentId == agentId)
                                 .OrderBy(m => m.UtcCreatedAt)
                                 .ToListAsync(cancellationToken);
        }

        public async Task<AgentMessage> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentNullException(nameof(messageId));
            return await _context.AgentMessages
                                 .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
                                 ?? throw new KeyNotFoundException($"Message with Id '{messageId}' not found.");
        }
    }
}
