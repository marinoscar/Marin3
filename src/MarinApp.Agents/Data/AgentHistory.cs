using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Data
{
    /// <summary>
    /// Represents a history of agent messages within a session, maintaining both a list of <see cref="AgentItem"/> objects
    /// and a synchronized <see cref="ChatHistory"/> for chat completion operations.
    /// </summary>
    /// <remarks>
    /// This class extends <see cref="List{T}"/> to store <see cref="AgentItem"/> instances and ensures that
    /// the <see cref="ChatHistory"/> is kept in sync with the list operations. It is designed to be used in conversational
    /// agent scenarios where both the full message context and a chat history are required.
    /// </remarks>
    public class AgentHistory : List<AgentItem>
    {
        /// <summary>
        /// Gets or sets the chat history associated with the agent messages.
        /// </summary>
        /// <remarks>
        /// The <see cref="ChatHistory"/> is updated automatically when items are added, removed, or cleared from the <see cref="AgentHistory"/>.
        /// </remarks>
        public ChatHistory ChatHistory { get; set; } = new();

        /// <summary>
        /// Adds an <see cref="AgentItem"/> to the history and updates the <see cref="ChatHistory"/> accordingly.
        /// </summary>
        /// <param name="item">The <see cref="AgentItem"/> to add.</param>
        public new void Add(AgentItem item)
        {
            ChatHistory.Add(item.Content);
            base.Add(item);
        }

        /// <summary>
        /// Adds a range of <see cref="AgentItem"/> objects to the history and updates the <see cref="ChatHistory"/> for each item.
        /// </summary>
        /// <param name="items">The collection of <see cref="AgentItem"/> objects to add.</param>
        public new void AddRange(IEnumerable<AgentItem> items)
        {
            foreach (var item in items)
            {
                ChatHistory.Add(item.Content);
            }
            base.AddRange(items);
        }

        /// <summary>
        /// Removes all <see cref="AgentItem"/> objects from the history and clears the <see cref="ChatHistory"/>.
        /// </summary>
        public new void Clear()
        {
            ChatHistory.Clear();
            base.Clear();
        }

        /// <summary>
        /// Removes the specified <see cref="AgentItem"/> from the history and updates the <see cref="ChatHistory"/>.
        /// </summary>
        /// <param name="item">The <see cref="AgentItem"/> to remove.</param>
        public new void Remove(AgentItem item)
        {
            ChatHistory.Remove(item.Content);
            base.Remove(item);
        }

        /// <summary>
        /// Generates a formatted transcript of the chat history, including agent names and message content.
        /// </summary>
        /// <remarks>
        /// The transcript is formatted in Markdown, with each message sectioned by agent and separated by horizontal rules.
        /// This method iterates through all <see cref="AgentItem"/> objects in the history, extracting the agent's display name
        /// and the message content from each <see cref="AgentMessage"/>. The output is suitable for display or export as a readable
        /// chat log.
        /// </remarks>
        /// <returns>
        /// A <see cref="string"/> containing the full chat transcript in Markdown format, including agent names and their messages.
        /// </returns>
        public string GetTranscript()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Chat Transcript:");
            sb.AppendLine("---");
            foreach (var item in this)
            {
                sb.AppendLine("");
                sb.AppendLine($"## Agent: {item.AgentMessage.AgentName}");
                sb.AppendLine("");
                sb.AppendLine($"{item.AgentMessage.Content}");
                sb.AppendLine("");
                sb.AppendLine("---");
            }
            return sb.ToString();
        }
    }
}
