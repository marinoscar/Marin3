using MarinApp.Agents.Data;
using MarinApp.Agents.Functions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly;

namespace MarinApp.Agents
{
    
    ///<summary>
    /// Provides an abstract base class for OpenAI-powered conversational agents.
    /// <para>
    /// This class encapsulates the configuration and initialization of the Semantic Kernel with OpenAI chat completion support,
    /// including robust HTTP client retry policies, model selection, and API key management.
    /// </para>
    /// <para>
    /// Derived classes must implement <see cref="GetModelName"/> to specify the OpenAI model to use.
    /// </para>
    /// <para>
    /// The class also provides extension points for customizing the kernel builder via <see cref="OnBuildingKernel"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="OpenAIAgentBase"/> is designed for use in .NET 9 applications, leveraging dependency injection
    /// and configuration patterns. It ensures that all HTTP requests to OpenAI endpoints are resilient to transient failures
    /// by applying an exponential backoff retry policy using Polly.
    /// </para>
    /// <para>
    /// API keys are resolved in the following order:
    /// <list type="number">
    /// <item>Environment variables: <c>OPENAI_API_KEY</c>, <c>OPENAI_KEY</c>, <c>OPENAI_API</c></item>
    /// <item>App configuration: <c>OpenAI:ApiKey</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract class OpenAIAgentBase : KernelAgentBase
    {
        /// <summary>
        /// The application configuration instance used to resolve API keys and other settings.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIAgentBase"/> class.
        /// </summary>
        /// <param name="agentHistoryService">The service responsible for persisting and retrieving agent message history.</param>
        /// <param name="loggerFactory">The logger factory used to create a logger for this agent.</param>
        /// <param name="configuration">The application configuration instance.</param>
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
        /// Initializes and configures a Semantic Kernel instance for OpenAI chat completion with robust HTTP retry policies.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method sets up the Semantic Kernel with the following features:
        /// <list type="bullet">
        ///   <item>Configures an <see cref="HttpClient"/> with a Polly-based retry policy for resilient OpenAI API calls.</item>
        ///   <item>Injects the OpenAI chat completion service using the model name and API key provided by derived classes.</item>
        ///   <item>Adds debug-level logging to the kernel for detailed diagnostics.</item>
        ///   <item>Invokes <see cref="OnBuildingKernel"/> to allow further customization by subclasses.</item>
        ///   <item>Adds standard plugins (e.g., <see cref="DateFunctions"/>) to the kernel.</item>
        /// </list>
        /// </para>
        /// <para>
        /// If any step fails, the method logs the error and rethrows the exception.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A fully configured <see cref="Kernel"/> instance ready for use with OpenAI chat completion.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if kernel initialization fails at any stage.
        /// </exception>
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

                // Add debug logging for the kernel
                builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));

                // Allow derived classes to further customize the kernel builder
                OnBuildingKernel(builder);

                kernel = builder.Build();

                Logger?.LogDebug("Adding standard plugins to the kernel.");
                kernel.Plugins.AddFromObject(new DateFunctions());


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
        /// Extension point for derived classes to customize the <see cref="IKernelBuilder"/> before the kernel is built.
        /// <para>
        /// Override this method to add plugins, services, or other configuration to the kernel builder.
        /// </para>
        /// </summary>
        /// <param name="builder">The kernel builder instance to customize.</param>
        protected virtual void OnBuildingKernel(IKernelBuilder builder)
        {
            Logger?.LogDebug("OnBuildingKernel called in OpenAIAgentBase.");
        }

        /// <summary>
        /// Creates and returns a Polly asynchronous retry policy for HTTP requests.
        /// <para>
        /// The policy retries on <see cref="HttpRequestException"/>, HTTP 429 (Too Many Requests), and all 5xx server errors,
        /// using exponential backoff (2^attempt seconds) for up to 3 retries.
        /// </para>
        /// </summary>
        /// <returns>
        /// An <see cref="IAsyncPolicy{HttpResponseMessage}"/> instance for resilient HTTP requests.
        /// </returns>
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
        /// When implemented in a derived class, returns the OpenAI model name to use for chat completion.
        /// <para>
        /// Example: <c>"gpt-4"</c>, <c>"gpt-3.5-turbo"</c>, etc.
        /// </para>
        /// </summary>
        /// <returns>The model name as a string.</returns>
        protected abstract string GetModelName();

        /// <summary>
        /// Resolves the OpenAI API key from environment variables or application configuration.
        /// <para>
        /// The following environment variables are checked in order: <c>OPENAI_API_KEY</c>, <c>OPENAI_KEY</c>, <c>OPENAI_API</c>.
        /// If none are found, the configuration key <c>OpenAI:ApiKey</c> is used.
        /// </para>
        /// </summary>
        /// <returns>The OpenAI API key as a string.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the API key cannot be found in environment variables or configuration.
        /// </exception>
        protected virtual string GetApiKey()
        {
            var key = string.Empty;
            // Start with environment variable
            key = GetEnvValue("OPENAI_API_KEY") ??
                GetEnvValue("OPENAI_KEY") ??
                GetEnvValue("OPENAI_API");

            if (!string.IsNullOrWhiteSpace(key)) return key;

            // Then check app settings
            key = _configuration["OpenAI:ApiKey"];
            if (!string.IsNullOrEmpty(key)) return key;

            throw new InvalidOperationException("OpenAI API key not found in environment variables or configuration.");
        }

        /// <summary>
        /// Helper method to retrieve an environment variable value from process, user, or machine scope.
        /// </summary>
        /// <param name="key">The environment variable name.</param>
        /// <returns>The value of the environment variable, or <c>null</c> if not found.</returns>
        private string? GetEnvValue(string key)
        {
            return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process) ??
                Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User) ??
                Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Machine);
        }
    }
}
