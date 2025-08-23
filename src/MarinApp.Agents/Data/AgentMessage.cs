using MarinApp.Core.Entities;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Data
{
    public class AgentMessage : BaseEntity
    {
        public string SessionId { get; set; } = default!;
        public string AgentId { get; set; } = default!;
        public string AgentName { get; set; } = default!;
        public string Role { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string MimeType { get; set; } = "text/markdown";
        public string ModelId { get; set; } = default!;
        public string Metadata { get; set; } = "{}";

        public static AgentMessage Create(string sessionId, AgentBase agent, ChatMessageContent content)
        {
            return new AgentMessage
            {
                SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId)),
                AgentId = agent?.Id ?? throw new ArgumentNullException(nameof(agent)),
                AgentName = agent?.Name ?? throw new ArgumentNullException(nameof(agent)),
                Role = Convert.ToString(content.Role) ?? throw new ArgumentNullException(nameof(content.Role)),
                Content = content.Content ?? throw new ArgumentNullException(nameof(content.Content)),
                MimeType = content.MimeType ?? "text/markdown",
                ModelId = content.ModelId ?? throw new ArgumentNullException(nameof(content.ModelId)),
                Metadata = content.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(content.Metadata) : "{}"
            };
        }
        

    }
}
