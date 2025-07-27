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
    public class AppConfigurationService
    {

        private readonly AppDataContext _context;
        private readonly ILogger<AppConfigurationService> _logger;

        public AppConfigurationService(AppDataContext appDataContext, ILogger<AppConfigurationService> logger)
        {
            _context = appDataContext ?? throw new ArgumentNullException(nameof(appDataContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<IQueryable<AppConfiguration>> GetAppConfigurationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                return Task.Run(() => _context.AppConfiguration.AsQueryable(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all app configurations.");
                throw;
            }
        }

        public async Task<AppConfiguration?> GetAppConfigurationAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            try
            {
                return await _context.AppConfiguration
                    .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving app configuration for key '{Key}'.", key);
                throw;
            }
        }

        public async Task<AppConfiguration> AddOrUpdateAppConfigurationAsync(AppConfiguration configuration, CancellationToken cancellationToken)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding or updating app configuration for key '{Key}'.", configuration?.Key);
                throw;
            }
        }

        public async Task<bool> DeleteAppConfigurationAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            try
            {
                var config = await _context.AppConfiguration
                    .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
                if (config == null)
                    return false;
                _context.AppConfiguration.Remove(config);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting app configuration for key '{Key}'.", key);
                throw;
            }
        }
    }
}
