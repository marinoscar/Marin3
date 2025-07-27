using MarinApp.Core.Data;
using MarinApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Services
{
    /// <summary>
    /// Provides services for managing application configuration settings, including retrieval, addition, update, and deletion.
    /// </summary>
    public class AppConfigurationService
    {
        private readonly AppDataContext _context;
        private readonly ILogger<AppConfigurationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigurationService"/> class.
        /// </summary>
        /// <param name="appDataContext">The application's data context.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="appDataContext"/> or <paramref name="logger"/> is null.</exception>
        public AppConfigurationService(AppDataContext appDataContext, ILogger<AppConfigurationService> logger)
        {
            _context = appDataContext ?? throw new ArgumentNullException(nameof(appDataContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves all application configuration settings as an <see cref="IQueryable{AppConfiguration}"/>.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IQueryable{AppConfiguration}"/> of all configurations.</returns>
        public Task<IQueryable<AppConfiguration>> GetAppConfigurationsAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => _context.AppConfiguration.AsQueryable(), cancellationToken);
        }

        /// <summary>
        /// Retrieves a specific application configuration by its key.
        /// </summary>
        /// <param name="key">The unique key of the configuration setting.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the <see cref="AppConfiguration"/> if found; otherwise, <c>null</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null or whitespace.</exception>
        public async Task<AppConfiguration?> GetAppConfigurationAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            return await _context.AppConfiguration
                .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        }

        /// <summary>
        /// Adds a new or updates an existing application configuration setting.
        /// </summary>
        /// <param name="configuration">The configuration entity to add or update.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the added or updated <see cref="AppConfiguration"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
        public async Task<AppConfiguration> AddOrUpdateAppConfigurationAsync(AppConfiguration configuration, CancellationToken cancellationToken)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            var existingConfig = await _context.AppConfiguration
                .FirstOrDefaultAsync(c => c.Key == configuration.Key, cancellationToken);
            if (existingConfig != null)
            {
                existingConfig.Value = configuration.Value;
                existingConfig.Environment = configuration.Environment;
                _context.AppConfiguration.Update(existingConfig);
            }
            else
            {
                _context.AppConfiguration.Add(configuration);
            }
            await _context.SaveChangesAsync(cancellationToken);
            return configuration;
        }

        /// <summary>
        /// Deletes an application configuration setting by its key.
        /// </summary>
        /// <param name="key">The unique key of the configuration setting to delete.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains <c>true</c> if the configuration was deleted; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null or whitespace.</exception>
        public async Task<bool> DeleteAppConfigurationAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            var config = await _context.AppConfiguration
                .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
            if (config == null)
                return false;
            _context.AppConfiguration.Remove(config);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

    }
}
