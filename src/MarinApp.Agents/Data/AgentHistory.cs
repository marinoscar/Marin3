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
    }
}
