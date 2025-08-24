using MarinApp.Agents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents
{
    /// <summary>
    /// Represents a human proxy agent that interacts with the user via a colorized console interface.
    /// This agent is designed for command-line scenarios, providing colored output for agent prompts
    /// and capturing user responses from the console input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Usage:</b> Instantiate <see cref="ColorConsoleAgent"/> with the required dependencies and use it
    /// to facilitate conversational interactions in a console environment. The agent displays its prompts
    /// in cyan for better visibility and reads user input from the standard input stream.
    /// </para>
    /// <para>
    /// <b>Session Management:</b> Inherits session and history management from <see cref="HumanProxyBase"/>.
    /// </para>
    /// <para>
    /// <b>Customization:</b> Override <see cref="PrintAgentText(string)"/> to change the color or formatting
    /// of agent messages.
    /// </para>
    /// </remarks>
    public class ColorConsoleAgent : HumanProxyBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorConsoleAgent"/> class.
        /// </summary>
        /// <param name="agentHistoryService">
        /// The service responsible for persisting and retrieving agent message history.
        /// </param>
        /// <param name="loggerFactory">
        /// The logger factory used to create loggers for this agent instance.
        /// </param>
        public ColorConsoleAgent(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory) : base(agentHistoryService, loggerFactory)
        {
            SetAgentDetails("color-console-agent", "Color Console Agent", "An agent that interacts with the user via a color console interface.");
        }

        /// <summary>
        /// Waits for a human response via the console after displaying the agent's message in color.
        /// </summary>
        /// <param name="agentText">The text to display to the user as the agent's prompt.</param>
        /// <param name="history">The current chat history for the session.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the response.</param>
        /// <returns>The user's response as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="agentText"/> is null or empty.</exception>
        public override async Task<string> WaitOnHumanResponseAsync(string? agentText, ChatHistory history, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(agentText)) throw new ArgumentNullException(nameof(agentText));

            PrintAgentText(agentText);

            Console.Write("Your response: ");
            var response = Console.ReadLine();
            return await Task.FromResult(response ?? string.Empty);
        }

        /// <summary>
        /// Prints the agent's text to the console in cyan color for emphasis.
        /// </summary>
        /// <param name="text">The text to display.</param>
        protected void PrintAgentText(string text)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
            Console.ForegroundColor = previousColor;
        }
    }
}
