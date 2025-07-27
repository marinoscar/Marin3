using MarinApp.Core.Configuration;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Serilog;
using Serilog.Events;

namespace MarinApp.Configuration
{
    /// <summary>
    /// Provides extension methods for configuring application-wide services and features.
    /// </summary>
    public static class AppConfigurationExtensions
    {
        /// <summary>
        /// Configures logging for the application using Serilog and console logging.
        /// 
        /// This method performs the following:
        /// <list type="bullet">
        /// <item>Reads the default log level from configuration ("Logging:LogLevel:Default").</item>
        /// <item>Registers logging services and console logging with the dependency injection container.</item>
        /// <item>Initializes Serilog as the application's logger, writing logs to both the console and a PostgreSQL database table ("SeriLogs").</item>
        /// <item>Ensures the PostgreSQL log table is created if it does not exist.</item>
        /// <item>Configures the Serilog logger to read settings from the application's configuration and services, set the minimum log level, and enrich logs with context.</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The <see cref="WebApplicationBuilder"/> to configure.</param>
        /// <returns>The same <see cref="WebApplicationBuilder"/> instance for chaining.</returns>
        public static WebApplicationBuilder AddApplicationLogging(this WebApplicationBuilder builder)
        {
            var logLevelName = builder.Configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Information";
            var logLevel = Enum.TryParse(logLevelName, out LogEventLevel level) ? level : LogEventLevel.Information;
            builder.Services.AddLogging();
            builder.Logging.AddConsole();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.PostgreSQL(
                    connectionString: DbConnectionStringHelper.GetConnectionString(),
                    tableName: "SeriLogs",
                    needAutoCreateTable: true) // will create table if not exists
                .CreateLogger();

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .MinimumLevel.Is(logLevel)
                    .Enrich.FromLogContext()
                    .WriteTo.Console();
            });

            return builder;
        }
    }
}
