using MarinApp.Core.Data;
using MarinApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;


namespace MarinApp.Core.Services
{
    /// <summary>
    /// Service responsible for managing application configuration settings.
    /// Provides methods for retrieving, adding, updating, and deleting <see cref="AppConfiguration"/> entities.
    /// </summary>
    /// <remarks>
    /// This service uses an <see cref="AppDataContext"/> instance for data access and an <see cref="ILogger"/> for logging errors.
    /// </remarks>
    public class AppConfigurationService
    {
        // Factory for creating new AppDataContext instances.
        private readonly IDbContextFactory<AppDataContext> _contextFactory;
        // The current AppDataContext instance used for data operations.
        private readonly AppDataContext _context;
        // Logger for capturing errors and informational messages.
        private readonly ILogger<AppConfigurationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigurationService"/> class.
        /// </summary>
        /// <param name="contextFactory">The factory used to create <see cref="AppDataContext"/> instances.</param>
        /// <param name="logger">The logger used for logging errors and information.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="contextFactory"/> or <paramref name="logger"/> is null.</exception>
        public AppConfigurationService(IDbContextFactory<AppDataContext> contextFactory, ILogger<AppConfigurationService> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _context = _contextFactory.CreateDbContext();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves all <see cref="AppConfiguration"/> entities as an <see cref="IQueryable{AppConfiguration}"/>.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an <see cref="IQueryable{AppConfiguration}"/>
        /// representing all configuration entities in the database.
        /// </returns>
        /// <exception cref="Exception">Throws if an error occurs during retrieval.</exception>
        public Task<IQueryable<AppConfiguration>> GetAll(CancellationToken cancellationToken)
        {
            try
            {
                // Return all AppConfiguration entities as a queryable collection.
                return Task.Run(() => _context.AppConfiguration.AsQueryable(), cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the error and rethrow for upstream handling.
                _logger.LogError(ex, "Error retrieving all app configurations.");
                throw;
            }
        }

        /// <summary>
        /// Retrieves <see cref="AppConfiguration"/> entities that match the specified filter expression.
        /// </summary>
        /// <param name="expression">A LINQ expression to filter the configurations.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an <see cref="IQueryable{AppConfiguration}"/>
        /// of configurations matching the filter.
        /// </returns>
        /// <exception cref="Exception">Throws if an error occurs during retrieval.</exception>
        public Task<IQueryable<AppConfiguration>> Get(Expression<Func<AppConfiguration, bool>> expression, CancellationToken cancellationToken = default)
        {
            try
            {
                // Return filtered AppConfiguration entities as a queryable collection.
                return Task.Run(() => _context.AppConfiguration.Where(expression), cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the error and rethrow for upstream handling.
                _logger.LogError(ex, "Error retrieving the app configurations.");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a single <see cref="AppConfiguration"/> entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the configuration entity.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching <see cref="AppConfiguration"/>,
        /// or <c>null</c> if not found.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="id"/> is null or empty.</exception>
        /// <exception cref="Exception">Throws if an error occurs during retrieval.</exception>
        public async Task<AppConfiguration?> GetById(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentOutOfRangeException(nameof(id), "ID must be greater than zero.");
            try
            {
                // Find the configuration entity by its Id.
                return await _context.AppConfiguration
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the error and rethrow for upstream handling.
                _logger.LogError(ex, "Error retrieving app configuration with ID '{Id}'.", id);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a single <see cref="AppConfiguration"/> entity by its configuration key.
        /// </summary>
        /// <param name="key">The unique key of the configuration entity.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching <see cref="AppConfiguration"/>,
        /// or <c>null</c> if not found.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null or whitespace.</exception>
        /// <exception cref="Exception">Throws if an error occurs during retrieval.</exception>
        public async Task<AppConfiguration?> GetByKey(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            try
            {
                // Find the configuration entity by its Key.
                return await _context.AppConfiguration
                    .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the error and rethrow for upstream handling.
                _logger.LogError(ex, "Error retrieving app configuration for key '{Key}'.", key);
                throw;
            }
        }

        /// <summary>
        /// Adds a new <see cref="AppConfiguration"/> entity or updates an existing one.
        /// If the entity exists (matched by Id), its value, environment, version, and update timestamp are updated.
        /// Otherwise, a new entity is added.
        /// </summary>
        /// <param name="configuration">The configuration entity to add or update.</param>
        /// <returns>The added or updated <see cref="AppConfiguration"/> entity.</returns>
        /// <exception cref="Exception">Throws if an error occurs during add or update.</exception>
        public AppConfiguration AddOrUpdate(AppConfiguration configuration)
        {
            try
            {
                // Attempt to find an existing configuration by Id.
                var itemInDb = _context.AppConfiguration.FirstOrDefault(i => i.Id == configuration.Id);
                if (itemInDb != null)
                {
                    // Update existing entity fields.
                    itemInDb.Version = itemInDb.Version + 1;
                    itemInDb.UtcUpdatedAt = DateTime.UtcNow;
                    itemInDb.Value = configuration.Value;
                    itemInDb.Environment = configuration.Environment;
                    // Mark entity as updated in the context.
                    _context.Update(itemInDb);
                    configuration = itemInDb;
                }
                else
                {
                    // Add new configuration entity.
                    _context.Add(configuration);
                }
                // Persist changes to the database.
                _context.SaveChanges();
                return configuration;
            }
            catch (Exception ex)
            {
                // Log the error and rethrow for upstream handling.
                _logger.LogError(ex, "Error adding or updating app configuration for id '{Id}'.", configuration?.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a <see cref="AppConfiguration"/> entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the configuration entity to delete.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is <c>true</c> if the entity was deleted;
        /// <c>false</c> if no entity was found with the specified identifier.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="id"/> is null or whitespace.</exception>
        /// <exception cref="Exception">Throws if an error occurs during deletion.</exception>
        public async Task<bool> Delete(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));
            try
            {
                // Find the configuration entity by its Key (should this be Id?).
                var config = await _context.AppConfiguration
                    .FirstOrDefaultAsync(c => c.Key == id, cancellationToken);
                if (config == null)
                    return false;
                // Remove the entity from the context.
                _context.AppConfiguration.Remove(config);
                // Persist changes to the database.
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                // Log the error and rethrow for upstream handling.
                _logger.LogError(ex, "Error deleting app configuration for id '{Id}'.", id);
                throw;
            }
        }
    }
}
