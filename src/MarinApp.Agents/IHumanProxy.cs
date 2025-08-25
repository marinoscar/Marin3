using Microsoft.SemanticKernel.ChatCompletion;

namespace MarinApp.Agents
{
    /// <summary>
    /// Defines the contract for a human proxy agent that facilitates direct interaction between the system and a human user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="IHumanProxy"/> interface extends <see cref="IAgent"/> to provide additional methods for
    /// interacting with a human user in conversational scenarios. Implementations of this interface are responsible
    /// for displaying messages to the user and awaiting user input, typically within a chat or conversational UI.
    /// </para>
    /// <para>
    /// <b>Key Responsibilities:</b>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="PrintMessage(string, string)"/>: Displays a message to the human user, supporting various MIME types for content formatting.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="WaitOnHumanResponseAsync(string?, ChatHistory, CancellationToken)"/>: Asynchronously waits for a response from the human user,
    ///       optionally providing context or a prompt, and returns the user's input as a string.
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage Scenario:</b>
    /// <br/>
    /// This interface is typically implemented by components that bridge automated agents and human users, such as
    /// chat UIs, conversational bots with human-in-the-loop workflows, or systems requiring explicit user confirmation
    /// or input during an automated process.
    /// </para>
    /// </remarks>
    public interface IHumanProxy : IAgent
    {
        /// <summary>
        /// Displays a message to the human user.
        /// </summary>
        /// <param name="content">
        /// The message content to display. This may include plain text, markdown, or other formats depending on the <paramref name="mimeType"/>.
        /// </param>
        /// <param name="mimeType">
        /// The MIME type of the content (e.g., <c>text/plain</c>, <c>text/markdown</c>, <c>text/html</c>), which determines how the message is rendered to the user.
        /// </param>
        /// <remarks>
        /// Implementations should ensure that the message is presented to the user in a manner consistent with the specified MIME type.
        /// </remarks>
        void PrintMessage(string content, string mimeType);

        /// <summary>
        /// Asynchronously waits for a response from the human user.
        /// </summary>
        /// <param name="agentText">
        /// Optional text or prompt from the agent to display to the user, providing context for the expected response.
        /// </param>
        /// <param name="history">
        /// The current <see cref="ChatHistory"/> of the conversation, which may be used to provide additional context or display previous messages to the user.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the wait operation if needed (e.g., if the session is terminated or times out).
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, with the result being the user's response as a string.
        /// </returns>
        /// <remarks>
        /// Implementations should block or suspend execution until the user provides input, or the operation is cancelled.
        /// The returned string should contain the user's raw input, which may be further processed by the agent or system.
        /// </remarks>
        Task<string> WaitOnHumanResponseAsync(string? agentText, ChatHistory history, CancellationToken cancellationToken);
    }
}