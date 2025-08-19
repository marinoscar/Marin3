using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

/// <summary>
/// Application entry point
/// This class sets up dependency injection, logging, and provides
/// a structured way to run console actions with error handling and logging.
/// </summary>
/// <remarks>
/// Requires the nuget package: Microsoft.Extensions.Logging.Console
/// </remarks>
class Program
{
    /// <summary>
    /// The logger instance used for logging messages to the console.
    /// </summary>
    private static ILogger _logger;

    /// <summary>
    /// The service collection used for dependency injection setup.
    /// </summary>
    private static IServiceCollection _serviceCollection;

    /// <summary>
    /// The service provider built from the service collection.
    /// </summary>
    private static IServiceProvider _serviceProvider;

    /// <summary>
    /// Main entry point for the application.
    /// Initializes services, logging, parses arguments, and executes the main action.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    static void Main(string[] args)
    {
        var serviceProvider = new ServiceCollection();
        _serviceCollection = serviceProvider;
        var arguments = new ConsoleOptions(args);
        
        InitializeLogger();

        _serviceProvider = _serviceCollection.BuildServiceProvider();
        _serviceProvider.CreateScope();

        var factory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = factory.CreateLogger("MarinApp");
        RunAction(() =>
        {
            DoAction(arguments);
        }, true);
    }

    /// <summary>
    /// Executes the main logic of the application.
    /// </summary>
    /// <param name="arguments">Parsed command-line options.</param>
    static void DoAction(ConsoleOptions arguments)
    {
        WriteLineInfo("Hello World");
    }

    /// <summary>
    /// Runs the provided action, handling exceptions and optionally waiting for a key press at the end.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="waitForKey">If true, waits for a key press after execution.</param>
    public static void RunAction(Action action, bool waitForKey = false)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            WriteLineError(exception.ToString());
        }
        finally
        {
            if (waitForKey)
            {
                WriteLineInfo("Press any key to end");
                Console.ReadKey();
            }
        }
    }

    #region Console Methods

    /// <summary>
    /// Logs an informational message with formatting.
    /// </summary>
    /// <param name="format">Message format string.</param>
    /// <param name="arg">Arguments for the format string.</param>
    public static void WriteLineInfo(string format, params object[] arg)
    {
        _logger.LogInformation(format, arg);
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public static void WriteLineInfo(string message)
    {
        _logger.LogInformation(message);
    }

    /// <summary>
    /// Logs a warning message with formatting.
    /// </summary>
    /// <param name="format">Message format string.</param>
    /// <param name="arg">Arguments for the format string.</param>
    public static void WriteLineWarning(string format, params object[] arg)
    {
        _logger.LogWarning(format, arg);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public static void WriteLineWarning(string message)
    {
        _logger.LogWarning(message);
    }

    /// <summary>
    /// Logs an error message with formatting.
    /// </summary>
    /// <param name="format">Message format string.</param>
    /// <param name="arg">Arguments for the format string.</param>
    public static void WriteLineError(string format, params object[] arg)
    {
        _logger.LogError(format, arg);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public static void WriteLineError(string message)
    {
        _logger.LogError(message);
    }

    /// <summary>
    /// Configures and adds logging services to the service collection.
    /// Sets up console logging with custom formatting and color behavior.
    /// </summary>
    private static void InitializeLogger()
    {
        _serviceCollection.AddLogging(builder =>
        {
            builder.AddConsole(options =>
            {
                // Enable colors for the console logger
                options.LogToStandardErrorThreshold = LogLevel.Debug; // Example: log warnings and above to stderr
                options.FormatterName = "simple"; // Use the default simple formatter
            }).AddSimpleConsole(o =>
            {
                o.SingleLine = true; // Write each log message on a single line
                o.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; // Custom timestamp format
                o.IncludeScopes = true; // Include scopes in log messages
                o.ColorBehavior = LoggerColorBehavior.Enabled; // Enable colors in the console output
                o.UseUtcTimestamp = false; // Use UTC for timestamps
            });
        });
    }

    #endregion
}

/// <summary>
/// Provides an abstraction to handle common console switches and arguments.
/// </summary>
public class ConsoleOptions
{
    private List<string> _args;

    /// <summary>
    /// Creates an instance of the class, storing the provided arguments.
    /// </summary>
    /// <param name="args">Collection of arguments.</param>
    public ConsoleOptions(IEnumerable<string> args)
    {
        _args = new List<string>(args);
    }

    /// <summary>
    /// Gets the value for the switch in the argument collection.
    /// </summary>
    /// <param name="name">The name of the switch.</param>
    /// <returns>The switch value if present, otherwise null.</returns>
    public string this[string name]
    {
        get
        {
            var idx = _args.IndexOf(name);
            if (idx == (_args.Count - 1)) return null;
            return _args[idx + 1];
        }
    }

    /// <summary>
    /// Indicates if the switch exists in the argument collection.
    /// </summary>
    /// <param name="name">The name of the switch.</param>
    /// <returns>True if the switch name is in the collection, otherwise false.</returns>
    public bool ContainsSwitch(string name)
    {
        return _args.Contains(name);
    }
}