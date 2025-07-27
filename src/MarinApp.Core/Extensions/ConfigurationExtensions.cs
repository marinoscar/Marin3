using MarinApp.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="IConfigurationBuilder"/> to support
    /// loading configuration values from a database using a custom configuration provider.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds a custom database-backed configuration provider to the <see cref="IConfigurationBuilder"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IConfigurationBuilder"/> to add the provider to.
        /// </param>
        /// <param name="optionsAction">
        /// An action to configure the <see cref="DbContextOptionsBuilder"/> for the database context
        /// used by the configuration provider.
        /// </param>
        /// <param name="environment">
        /// The environment name (e.g., "Development", "Production") to filter configuration values.
        /// </param>
        /// <returns>
        /// The same <see cref="IConfigurationBuilder"/> instance so that additional calls can be chained.
        /// </returns>
        /// <remarks>
        /// This extension method allows you to load configuration values from a database source,
        /// enabling dynamic and environment-specific configuration management.
        /// </remarks>
        public static IConfigurationBuilder AddDbConfigurationProvider(
            this IConfigurationBuilder builder,
            Action<DbContextOptionsBuilder> optionsAction,
            string environment)
        {
            return builder.Add(new DbConfigurationSource(optionsAction, environment));
        }
    }
}
