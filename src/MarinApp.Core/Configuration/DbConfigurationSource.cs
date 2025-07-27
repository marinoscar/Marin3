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
    /// Represents a configuration source that loads configuration values from a database using Entity Framework Core.
    /// This source is intended to be used with <see cref="DbConfigurationProvider"/> to provide configuration
    /// values from the <c>AppConfiguration</c> table, supporting both shared and environment-specific settings.
    /// </summary>
    /// <remarks>
    /// The <see cref="DbConfigurationSource"/> is typically added to an <see cref="IConfigurationBuilder"/>
    /// to enable loading configuration values from a database. It requires an action to configure the
    /// <see cref="DbContextOptionsBuilder"/> for the database context and the name of the environment
    /// (e.g., "Development", "Staging", "Production").
    /// </remarks>
    public class DbConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// The action used to configure the <see cref="DbContextOptionsBuilder"/> for the database context.
        /// </summary>
        private readonly Action<DbContextOptionsBuilder> _optionsAction;

        /// <summary>
        /// The environment name for which to load configuration values.
        /// </summary>
        private readonly string _environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbConfigurationSource"/> class.
        /// </summary>
        /// <param name="optionsAction">
        /// An action to configure the <see cref="DbContextOptionsBuilder"/> for the database context.
        /// </param>
        /// <param name="environment">
        /// The environment name (e.g., "Development", "Staging", "Production") for which to load configuration values.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="optionsAction"/> or <paramref name="environment"/> is <c>null</c>.
        /// </exception>
        public DbConfigurationSource(Action<DbContextOptionsBuilder> optionsAction, string environment)
        {
            _optionsAction = optionsAction ?? throw new ArgumentNullException(nameof(optionsAction));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Builds the <see cref="DbConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The configuration builder.</param>
        /// <returns>
        /// A <see cref="DbConfigurationProvider"/> that loads configuration values from the database
        /// for the specified environment.
        /// </returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DbConfigurationProvider(_optionsAction, _environment);
        }
    }
}
