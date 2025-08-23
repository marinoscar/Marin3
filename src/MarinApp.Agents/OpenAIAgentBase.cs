using MarinApp.Agents.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly;

namespace MarinApp.Agents
{
    public abstract class OpenAIAgentBase : AgentBase
    {
        private readonly IConfiguration _configuration;

        public OpenAIAgentBase(IAgentHistoryService agentHistoryService, ILoggerFactory loggerFactory, IConfiguration configuration)
            : base(agentHistoryService, loggerFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        }
        protected override Kernel InitializeKernel()
        {
            var builder = Kernel.CreateBuilder();

            // Configure HttpClient with retry policy
            var services = builder.Services;

            services.AddHttpClient("OpenAIWithRetry")
                .AddPolicyHandler(GetRetryPolicy());

            Kernel kernel = null;
            try
            {
                // Build service provider temporarily to get HttpClient
                using var serviceProvider = services.BuildServiceProvider();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("OpenAIWithRetry");

                Logger?.LogDebug("Creating OpenAI chat completion with model: {ModelName}", GetModelName());

                builder.AddOpenAIChatCompletion(
                    modelId: GetModelName(),
                    apiKey: GetApiKey(),
                    httpClient: httpClient
                );

                builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));

                OnBuildingKernel(builder);

                kernel = builder.Build();

                Logger?.LogDebug("Kernel successfully built for OpenAIAgentBase.");
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to initialize Kernel in OpenAIAgentBase: {Message}", ex.Message);
                throw;
            }

            return kernel;
        }

        protected virtual void OnBuildingKernel(IKernelBuilder builder)
        {
            // Allow derived classes to customize the kernel builder if needed
            Logger?.LogDebug("OnBuildingKernel called in OpenAIAgentBase.");
        }

        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                               (int)r.StatusCode >= 500) // server errors
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // exponential backoff
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to {outcome?.Exception?.Message ?? outcome?.Result?.StatusCode.ToString()}");
                    });
        }

        protected abstract string GetModelName();

        protected virtual string GetApiKey()
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
