using MarinApp.Agents.Data;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents
{
    public class OpenAIAgentBase : AgentBase
    {
        private readonly IConfiguration _configuration;

        public OpenAIAgentBase(Kernel kernel, IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory, IConfiguration configuration)
            : base(kernel, agentHistoryService, loggerFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var apiKey = TryGetOpenAIKey();

        }


        protected virtual Kernel InitializeKernel()
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("", TryGetOpenAIKey());
            builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));

        }

        protected virtual string TryGetOpenAIKey()
        {
            var key = string.Empty;
            // Start with environment variable
            key = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User) ??
                Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.Machine);

            if (!string.IsNullOrWhiteSpace(key)) return key;

            // Then check app settings
            key = _configuration["OpenAI:ApiKey"];
            if (!string.IsNullOrEmpty(key)) return key;
            
            throw new InvalidOperationException("OpenAI API key not found in environment variables or configuration.");
        }
    }
}
