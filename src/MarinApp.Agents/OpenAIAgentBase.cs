using MarinApp.Agents.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly;

namespace MarinApp.Agents
{
    /// <summary>
    /// Provides a base class for OpenAI-powered conversational agents, encapsulating kernel initialization,
    /// HTTP client configuration with retry policies, and API key management.
    /// <para>
    /// This class is intended to be inherited by concrete agent implementations that interact with OpenAI models
    /// via the Microsoft Semantic Kernel. It ensures robust communication with OpenAI endpoints by configuring
    /// an <see cref="HttpClient"/> with an exponential backoff retry policy and supports flexible API key retrieval
    /// from environment variables or application configuration.
    /// </para>
    /// <para>
    /// Derived classes must implement <see cref="GetModelName"/> to specify the OpenAI model to use.
    /// </para>
    /// </summary>
    public abstract class OpenAIAgentBase : AgentBase
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIAgentBase"/> class.
        /// </summary>
        /// <param name="agentHistoryService">The service responsible for persisting and retrieving agent message history.</param>
        /// <param name="loggerFactory">The logger factory used to create a logger for this agent.</param>
        /// <param name="configuration">The application configuration for retrieving API keys and settings.</param>
        /// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
        public OpenAIAgentBase(
            IAgentHistoryService agentHistoryService,
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
            : base(agentHistoryService, loggerFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Initializes and returns a new instance of the <see cref="Kernel"/> used by the agent.
        /// <para>
        /// Configures an <see cref="HttpClient"/> with a retry policy for resilient OpenAI API calls,
        /// sets up logging, and registers the OpenAI chat completion service with the specified model and API key.
        /// </para>
        /// </summary>
        /// <returns>A fully initialized <see cref="Kernel"/> instance for semantic operations.</returns>
        /// <exception cref="Exception">Thrown if kernel initialization fails.</exception>
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

        /// <summary>
        /// Allows derived classes to customize the kernel builder before the kernel is built.
        /// <para>
        /// Override this method to add plugins, configure additional services, or modify the kernel builder as needed.
        /// </para>
        /// </summary>
        /// <param name="builder">The kernel builder instance to customize.</param>
        protected virtual void OnBuildingKernel(IKernelBuilder builder)
        {
            // Allow derived classes to customize the kernel builder if needed
            Logger?.LogDebug("OnBuildingKernel called in OpenAIAgentBase.");
        }

        /// <summary>
        /// Creates and returns an asynchronous retry policy for HTTP requests.
        /// <para>
        /// The policy retries on <see cref="HttpRequestException"/>, HTTP 429 (Too Many Requests), and server errors (HTTP 5xx),
        /// using exponential backoff with up to 3 retries.
        /// </para>
        /// </summary>
        /// <returns>An asynchronous retry policy for <see cref="HttpResponseMessage"/>.</returns>
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

        /// <summary>
        /// When implemented in a derived class, returns the name or identifier of the OpenAI model to use for chat completion.
        /// </summary>
        /// <returns>The model name or identifier.</returns>
        protected abstract string GetModelName();

        /// <summary>
        /// Retrieves the OpenAI API key from environment variables or application configuration.
        /// <para>
        /// Checks the "OPENAI_API_KEY" environment variable (process, user, and machine scopes) first,
        /// then falls back to the "OpenAI:ApiKey" setting in the application configuration.
        /// </para>
        /// </summary>
        /// <returns>The OpenAI API key as a string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the API key is not found.</exception>
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
