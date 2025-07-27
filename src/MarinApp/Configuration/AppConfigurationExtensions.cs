using Luval.AuthMate.Core;
using Luval.AuthMate.Infrastructure.Configuration;
using Luval.AuthMate.Infrastructure.Data;
using Luval.AuthMate.Infrastructure.Logging;
using Luval.AuthMate.Postgres;
using MarinApp.Core.Configuration;
using MarinApp.Core.Data;
using MarinApp.Core.Extensions;
using Microsoft.EntityFrameworkCore;
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

        /// <summary>
        /// Adds and configures authentication services for the application, including AuthMate and Google OAuth.
        /// 
        /// This method performs the following steps:
        /// <list type="number">
        /// <item>Builds a temporary service provider to retrieve the application <see cref="IConfiguration"/> instance.</item>
        /// <item>Retrieves the database connection string using <see cref="DbConnectionStringHelper.GetConnectionString"/>.</item>
        /// <item>Creates a new <see cref="PostgresAuthMateContext"/> for AuthMate data storage.</item>
        /// <item>Registers AuthMate services with the dependency injection container, providing the bearing token key and a factory for the PostgreSQL context.</item>
        /// <item>Configures Google OAuth authentication using values from configuration:
        /// <list type="bullet">
        /// <item><c>OAuthProviders:Google:ClientId</c> (required)</item>
        /// <item><c>OAuthProviders:Google:ClientSecret</c> (required)</item>
        /// <item>Sets the login path to <c>/api/auth</c></item>
        /// </list>
        /// </item>
        /// <item>Initializes the AuthMate database and seeds it with the owner email (from <c>OAuthProviders:Google:OwnerEmail</c> in configuration) and required initial records.</item>
        /// <item>Logs a success message if configuration is successful, or logs and rethrows any exceptions encountered.</item>
        /// </list>
        /// </summary>
        /// <param name="s">The <see cref="IServiceCollection"/> to add authentication services to.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if required configuration values are missing.</exception>
        /// <exception cref="Exception">Rethrows any exception encountered during configuration.</exception>
        public static IServiceCollection AddApplicationAuth(this IServiceCollection s)
        {
            try
            {
                var config = s.BuildServiceProvider().GetRequiredService<IConfiguration>();
                var connString = DbConnectionStringHelper.GetConnectionString();


                // Add the database context for AuthMate
                s.AddAuthMateServices(
                    // The key to use for the bearing token implementation
                    config["AuthMate:BearingTokenKey"] ?? "No_Token",
                (s) =>
                {
                    // Returns the postgresql implementation
                    return new PostgresAuthMateContext(connString);
                });

                // Adds the Google Authentication
                s.AddAuthMateAuthentication(new GoogleOAuthConfiguration()
                {
                    // Client ID from your config file
                    ClientId = config["OAuthProviders:Google:ClientId"] ?? throw new ArgumentNullException("The Google client id is required"),
                    // Client secret from your config file
                    ClientSecret = config["OAuthProviders:Google:ClientSecret"] ?? throw new ArgumentNullException("The Google client secret is required"),
                    // Set the login path in the controller and pass the provider name
                    LoginPath = "/api/auth",
                });

                // Creates the context
                var contextHelper = new AuthMateContextHelper(
                        new PostgresAuthMateContext(connString),
                        new ColorConsoleLogger<AuthMateContextHelper>());

                // Ensure the database is created and initialize it with the owner email and required initial records
                contextHelper.InitializeDbAsync(config["OAuthProviders:Google:OwnerEmail"] ?? "")
                    .GetAwaiter()
                    .GetResult();

                Log.Information("Application authentication services configured successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while configuring application authentication services.");
                throw;
            }
            return s;
        }

        public static WebApplicationBuilder AddAppConfigurationProvider(this WebApplicationBuilder builder)
        {
            // Setup the configuration data source
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var connString = DbConnectionStringHelper.GetConnectionString();
            var serviceProvider = builder.Services.BuildServiceProvider();
            var dataContext = serviceProvider.GetRequiredService<AppDataContext>();

            builder.Configuration.AddDbConfigurationProvider(
                dataContext,
                env);
            return builder;
        }
    }
}
