using MarinApp.Agents.Data;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Orchestration
{
    /// <summary>
    /// Orchestrates a conversational chat session between a human user (via <see cref="IHumanProxy"/>) and an automated agent (<see cref="IAgent"/>).
    /// <para>
    /// This class is responsible for managing the lifecycle of a chat session, relaying messages between the human and the agent, and determining when the conversation should end.
    /// </para>
    /// <para>
    /// <b>Session Management:</b> Upon instantiation, a new session identifier is generated and set for both the human proxy and the agent, ensuring that all messages are associated with the same session context.
    /// </para>
    /// <para>
    /// <b>Chat Flow:</b> The <see cref="StartChat"/> method initiates the conversation loop, sending the initial message to the human, relaying responses to the agent, and displaying agent replies back to the user.
    /// The loop continues until the provided <paramref name="endSequence"/> delegate returns <c>true</c> for a given <see cref="AgentMessage"/>, indicating that the conversation should terminate.
    /// </para>
    /// <para>
    /// <b>Usage Example:</b>
    /// <code>
    /// var orchestrator = new ChatWithHuman(humanProxy, agent);
    /// orchestrator.StartChat("Hello! How can I help you today?", msg => msg.Content.Contains("bye"));
    /// </code>
    /// </para>
    /// </summary>
    public class ChatWithHuman
    {
        /// <summary>
        /// Gets the <see cref="IHumanProxy"/> instance representing the human participant in the chat.
        /// </summary>
        protected IHumanProxy HumanAgent { get; private set; } = default!;

        /// <summary>
        /// Gets the <see cref="IAgent"/> instance representing the automated agent participant in the chat.
        /// </summary>
        protected IAgent Agent { get; private set; } = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatWithHuman"/> class, establishing a shared session for both the human and agent participants.
        /// </summary>
        /// <param name="humanAgent">The <see cref="IHumanProxy"/> implementation that facilitates communication with the human user.</param>
        /// <param name="agent">The <see cref="IAgent"/> implementation that provides automated responses.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="humanAgent"/> or <paramref name="agent"/> is <c>null</c>.</exception>
        /// <remarks>
        /// A new session identifier is generated and set for both the human proxy and the agent, ensuring that all subsequent messages are tracked under the same session context.
        /// </remarks>
        public ChatWithHuman(IHumanProxy humanAgent, IAgent agent)
        {
            HumanAgent = humanAgent ?? throw new ArgumentNullException(nameof(humanAgent));
            Agent = agent ?? throw new ArgumentNullException(nameof(agent));
            var session = Guid.NewGuid().ToString();
            HumanAgent.SetSession(session);
            Agent.SetSession(session);
        }

        /// <summary>
        /// Starts a synchronous chat loop between the human and the agent, relaying messages until the end condition is met.
        /// </summary>
        /// <param name="initialMessage">
        /// The initial message to present to the human user. This message is sent as the first prompt in the conversation.
        /// </param>
        /// <param name="endSequence">
        /// A delegate that receives each <see cref="AgentMessage"/> from the human and returns <c>true</c> if the conversation should end, or <c>false</c> to continue.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="initialMessage"/> is <c>null</c> or empty.</exception>
        /// <remarks>
        /// <para>
        /// The method operates synchronously, blocking the calling thread until the conversation ends. It is suitable for console or test scenarios where blocking is acceptable.
        /// </para>
        /// <para>
        /// The conversation proceeds as follows:
        /// <list type="number">
        ///   <item>
        ///     <description>The initial message is sent to the human via <see cref="IHumanProxy.SendMessageAsync(string, PromptExecutionSettings?, CancellationToken)"/>.</description>
        ///   </item>
        ///   <item>
        ///     <description>The human's response is checked against the <paramref name="endSequence"/> delegate. If <c>true</c>, the loop exits.</description>
        ///   </item>
        ///   <item>
        ///     <description>The human's response is sent to the agent via <see cref="IAgent.SendMessageAsync(string, PromptExecutionSettings?, CancellationToken)"/>.</description>
        ///   </item>
        ///   <item>
        ///     <description>The agent's response is displayed to the human using <see cref="IHumanProxy.PrintAgentMessage(string, string)"/>.</description>
        ///   </item>
        ///   <item>
        ///     <description>The loop repeats, using the agent's response as the next prompt for the human.</description>
        ///   </item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Note:</b> This method uses <c>.GetAwaiter().GetResult()</c> to synchronously wait for asynchronous operations, which may cause deadlocks in certain environments (e.g., UI threads).
        /// </para>
        /// </remarks>
        public void StartChat(string initialMessage, Func<AgentMessage, bool> endSequence)
        {
            StartChatAsync(initialMessage, endSequence).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously starts a conversational chat loop between a human user (via <see cref="IHumanProxy"/>) and an automated agent (<see cref="IAgent"/>),
        /// relaying messages between the two participants until a specified end condition is met.
        /// </summary>
        /// <param name="initialMessage">
        /// The initial message to present to the human user at the start of the conversation. This message is sent as the first prompt in the chat session.
        /// </param>
        /// <param name="endSequence">
        /// A delegate that receives each <see cref="AgentMessage"/> generated from the human's response and returns <c>true</c> if the conversation should end,
        /// or <c>false</c> to continue the chat loop. This allows for custom logic to determine when the session should terminate (e.g., when the user says "bye").
        /// </param>
        /// <param name="cancellationToken">
        /// An optional <see cref="CancellationToken"/> that can be used to cancel the chat session asynchronously. If cancellation is requested, the method will exit promptly.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation. The task completes when the conversation ends, either by meeting the <paramref name="endSequence"/> condition or by cancellation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="initialMessage"/> is <c>null</c> or empty, or if <paramref name="endSequence"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method implements the main conversational loop for a chat session between a human and an agent. The flow is as follows:
        /// <list type="number">
        ///   <item>
        ///     <description>The <paramref name="initialMessage"/> is sent to the human user via <see cref="IHumanProxy.SendMessageAsync(string, PromptExecutionSettings?, CancellationToken)"/>.</description>
        ///   </item>
        ///   <item>
        ///     <description>The human's response is received as an <see cref="AgentMessage"/>.</description>
        ///   </item>
        ///   <item>
        ///     <description>The <paramref name="endSequence"/> delegate is invoked with the human's response. If it returns <c>true</c>, the conversation ends and the method returns.</description>
        ///   </item>
        ///   <item>
        ///     <description>If the conversation continues, the human's response is relayed to the agent via <see cref="IAgent.SendMessageAsync(string, PromptExecutionSettings?, CancellationToken)"/>.</description>
        ///   </item>
        ///   <item>
        ///     <description>The agent's reply is received as an <see cref="AgentMessage"/> and displayed to the human using <see cref="IHumanProxy.PrintAgentMessage(string, string)"/>.</description>
        ///   </item>
        ///   <item>
        ///     <description>The loop repeats, using the agent's reply as the next prompt for the human.</description>
        ///   </item>
        /// </list>
        /// </para>
        /// <para>
        /// The method is designed for use in asynchronous environments, such as Blazor or other UI frameworks, where blocking the calling thread is undesirable.
        /// It supports cancellation via the <paramref name="cancellationToken"/> parameter, allowing the session to be terminated gracefully if needed.
        /// </para>
        /// <para>
        /// <b>Example Usage:</b>
        /// <code language="csharp">
        /// var orchestrator = new HumanChatOrchestration(humanProxy, agent);
        /// await orchestrator.StartChatAsync("Hello! How can I help you today?", msg => msg.Content.Contains("bye"));
        /// </code>
        /// </para>
        /// <para>
        /// <b>Security Note:</b> Implementations of <see cref="IHumanProxy.PrintAgentMessage(string, string)"/> should sanitize and validate content based on the MIME type to prevent security issues such as XSS.
        /// </para>
        /// </remarks>
        public async Task StartChatAsync(string initialMessage, Func<AgentMessage, bool> endSequence, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(endSequence);
            if (string.IsNullOrEmpty(initialMessage)) throw new ArgumentNullException(nameof(initialMessage));

            // Send the initial or next message to the human and wait for their response.
            var humanResponse = await HumanAgent.SendMessageAsync(initialMessage, cancellationToken);

            while (true)
            {

                // Check if the end condition is met based on the human's response.
                if (endSequence(humanResponse)) return;

                // Relay the human's response to the agent and get the agent's reply.
                var agentResponse = await Agent.SendMessageAsync(humanResponse.Content, cancellationToken);

                // Display the agent's reply to the human user.
                HumanAgent.PrintAgentMessage(agentResponse.Content, agentResponse.MimeType);

                // Wait for human input based on the agent's reply.
                humanResponse = await HumanAgent.SendMessageAsync(string.Empty, null, cancellationToken);
            }
        }
    }
}
