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
    public class ColorConsoleAgent : HumanProxyBase
    {
        public ColorConsoleAgent(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory) : base(agentHistoryService, loggerFactory)
        {
        }
        public override async Task<string> WaitOnHumanResponseAsync(string? agentText, ChatHistory history, CancellationToken cancellationToken)
        {
            if(string.IsNullOrEmpty(agentText)) throw new ArgumentNullException(nameof(agentText));

            PrintAgentText(agentText);

            Console.Write("Your response: ");
            var response = Console.ReadLine();
            return await Task.FromResult(response ?? string.Empty);
        }

        protected void PrintAgentText(string text)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
            Console.ForegroundColor = previousColor;
        }
    }
}
