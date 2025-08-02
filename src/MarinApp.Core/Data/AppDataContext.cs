using MarinApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Data
{
    /// <summary>
    /// Represents the Entity Framework Core database context for the MarinApp application.
    /// Provides access to the application's data entities and manages database operations.
    /// </summary>
    public class AppDataContext : DbContext
    {

        private readonly ILogger<AppDataContext>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppDataContext"/> class using the specified options.
        /// </summary>
        /// <param name="options">The options to be used by the <see cref="DbContext"/>.</param>
        public AppDataContext(DbContextOptions<AppDataContext> options, ILogger<AppDataContext>? logger = null)
            : base(options) {
            _logger = logger;
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> representing the application configuration settings.
        /// </summary>
        public DbSet<AppConfiguration> AppConfiguration { get; set; } = null!;

        /// <summary>
        /// Configures the entity mappings and relationships for the data model.
        /// This method is called by the framework when the model for a derived context has been initialized,
        /// but before the model has been locked down and used to initialize the context.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppConfiguration>(e =>
            {
                e.Property(c => c.Key)
                    .IsRequired()
                    .HasMaxLength(100);
                // Additional configuration for AppConfiguration can be added here.
            });
        }

        public override void Dispose()
        {
            _logger?.LogInformation("Disposing AppDataContext.");
            base.Dispose();
        }
    }
}
