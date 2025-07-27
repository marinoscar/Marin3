using MarinApp.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Configuration
{
    /// <summary>
    /// Provides configuration values from a database source using Entity Framework Core.
    /// This provider loads configuration entries from the <see cref="AppConfiguration"/> table,
    /// supporting both shared (environment-agnostic) and environment-specific settings.
    /// Environment-specific settings override shared settings with the same key.
    /// </summary>
    public class DbConfigurationProvider : ConfigurationProvider
    {
        /// <summary>
        /// The action used to configure the <see cref="DbContextOptionsBuilder"/> for <see cref="AppDataContext"/>.
        /// </summary>
        private readonly Action<DbContextOptionsBuilder> _optionsAction;

        /// <summary>
        /// The environment name (e.g., "Development", "Staging", "Production") for which to load configuration.
        /// </summary>
        private readonly string _environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbConfigurationProvider"/> class.
        /// </summary>
        /// <param name="optionsAction">
        /// An action to configure the <see cref="DbContextOptionsBuilder"/> for the database context.
        /// </param>
        /// <param name="environment">
        /// The environment name for which to load configuration values.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="optionsAction"/> or <paramref name="environment"/> is null.
        /// </exception>
        public DbConfigurationProvider(Action<DbContextOptionsBuilder> optionsAction, string environment)
        {
            _optionsAction = optionsAction ?? throw new ArgumentNullException(nameof(optionsAction));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Loads configuration values from the database.
        /// This method ensures the configuration table exists, then loads all shared and environment-specific
        /// configuration entries. Environment-specific entries override shared entries with the same key.
        /// </summary>
        public override void Load()
        {
            var builder = new DbContextOptionsBuilder<AppDataContext>();
            _optionsAction(builder);

            using var dbContext = new AppDataContext(builder.Options);

            // Ensure the configuration table exists; creates it if it does not (using EF migrations if available).
            dbContext.Database.EnsureCreated();

            // Retrieve configuration entries: shared (Environment == null) and environment-specific.
            // Environment-specific entries override shared ones with the same key.
            var configEntries = dbContext.AppConfiguration
                .Where(e => e.Environment == null || e.Environment == _environment)
                .OrderBy(e => e.Environment == null ? 0 : 1) // Shared first, then environment-specific.
                .ToList();

            var data = new Dictionary<string, string>();
            foreach (var entry in configEntries)
            {
                // Environment-specific entries will overwrite shared ones with the same key.
                data[entry.Key] = entry.Value ?? string.Empty;
            }

            Data = data;
        }
    }
}
