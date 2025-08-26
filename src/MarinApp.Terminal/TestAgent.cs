using MarinApp.Agents;
using MarinApp.Agents.Data;
using MarinApp.Agents.Orchestration;
using MarinApp.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Terminal
{
    internal class TestAgent
    {
        private readonly IConfiguration _configuration;

        public TestAgent(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void Run()
        {
            Console.Clear();

            var logFactory = LoggerFactory.Create(builder => builder.AddEventLog().SetMinimumLevel(LogLevel.Error));
            //var storageContext = AgentDataContext.CreateNpgsql(DbConnectionStringHelper.GetConnectionString());
            var storageContext = AgentDataContext.CreateInMemory();
            storageContext.InitializeDb();
            var storageService = new AgentHistoryService(storageContext);

            var humanAgent = new ColorConsoleAgent(storageService, _configuration, logFactory);
            var aiAgent = new AIAgent(storageService, logFactory, _configuration);

            var chat = new ChatWithHuman(humanAgent, aiAgent);

            chat.StartChat("Hello how can I help you? when you are ready, just type done", (message) =>
            {
                return message.Content.Contains("done", StringComparison.OrdinalIgnoreCase);
            });

        }
    }

    internal class AIAgent : OpenAIAgentBase
    {
        public AIAgent(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory, IConfiguration configuration) : base(agentHistoryService, loggerFactory, configuration)
        {
            SetAgentDetails("openai-agent", "OpenAI Agent", "An AI agent that uses OpenAI's GPT models to generate responses.");
            SetSystemMessage("You are a helpful assistant.");
        }
    }
}
