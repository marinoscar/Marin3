using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Data
{
    public class AgentItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ChatMessageContent Content { get; set; } = default!;

        public AgentMessage AgentMessage { get; set; } = default!;
    }
}
