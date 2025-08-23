using Microsoft.EntityFrameworkCore;

namespace MarinApp.Agents.Data
{
    /// <summary>
    /// Entity Framework Core database context for agent-related data, including agent messages.
    /// Supports configuration for PostgreSQL, SQLite, and in-memory databases.
    /// </summary>
    public class AgentDataContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentDataContext"/> class using the specified options.
        /// </summary>
        /// <param name="options">The options to be used by the DbContext.</param>
        public AgentDataContext(DbContextOptions<AgentDataContext> options) : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for agent messages.
        /// </summary>
        public DbSet<AgentMessage> AgentMessages { get; set; } = default!;

        /// <summary>
        /// Configures the entity model for the context, including table mappings, property constraints, and indexes.
        /// </summary>
        /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AgentMessage>(entity =>
            {
                entity.ToTable("AgentMessages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                      .IsRequired()
                      .HasMaxLength(32);
                entity.Property(e => e.SessionId)
                      .IsRequired()
                      .HasMaxLength(64);
                entity.Property(e => e.AgentId)
                      .IsRequired()
                      .HasMaxLength(64);
                entity.Property(e => e.AgentName)
                      .IsRequired()
                      .HasMaxLength(128);
                entity.Property(e => e.Role)
                      .IsRequired()
                      .HasMaxLength(32);
                entity.Property(e => e.Content)
                      .IsRequired();
                entity.Property(e => e.MimeType)
                      .IsRequired()
                      .HasMaxLength(64)
                      .HasDefaultValue("text/markdown");
                entity.Property(e => e.ModelId)
                      .IsRequired()
                      .HasMaxLength(64);
                entity.Property(e => e.Metadata)
                      .IsRequired()
                      .HasDefaultValue("{}");
                entity.Property(e => e.UtcCreatedAt)
                      .IsRequired();
                entity.Property(e => e.UtcUpdatedAt)
                      .IsRequired();
                entity.Property(e => e.Version)
                      .IsRequired();
                entity.HasIndex(e => new { e.SessionId, e.AgentId });
            });
        }

        /// <summary>
        /// Creates a new instance of <see cref="AgentDataContext"/> configured for PostgreSQL using the provided connection string.
        /// </summary>
        /// <param name="connectionString">The PostgreSQL connection string.</param>
        /// <returns>A configured <see cref="AgentDataContext"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionString"/> is null or empty.</exception>
        public static AgentDataContext CreateNpgsql(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");

            var optionsBuilder = new DbContextOptionsBuilder<AgentDataContext>();
            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Optional: Configure Npgsql-specific options here
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });

            return new AgentDataContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Creates a new instance of <see cref="AgentDataContext"/> configured for SQLite using the provided connection string.
        /// </summary>
        /// <param name="connectionString">The SQLite connection string.</param>
        /// <returns>A configured <see cref="AgentDataContext"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionString"/> is null or empty.</exception>
        public static AgentDataContext CreateSqlite(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");

            var optionsBuilder = new DbContextOptionsBuilder<AgentDataContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new AgentDataContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Creates a new instance of <see cref="AgentDataContext"/> configured for an in-memory SQLite database.
        /// </summary>
        /// <returns>A configured <see cref="AgentDataContext"/> instance using an in-memory database.</returns>
        public static AgentDataContext CreateInMemory()
        {
            return CreateSqlite("Data Source=:memory:");
        }
    }

    /// <summary>
    /// Factory class for creating <see cref="AgentDataContext"/> instances based on the specified provider and connection string.
    /// </summary>
    public class AgentDataContextFactory
    {
        /// <summary>
        /// Creates a new <see cref="AgentDataContext"/> instance using the specified provider and connection string.
        /// </summary>
        /// <param name="provider">The database provider ("npgsql", "sqlite", or "inmemory").</param>
        /// <param name="connectionString">The connection string for the database provider.</param>
        /// <returns>A configured <see cref="AgentDataContext"/> instance.</returns>
        /// <exception cref="NotSupportedException">Thrown if the specified provider is not supported.</exception>
        public static AgentDataContext Create(string provider, string connectionString = default!)
        {
            return provider.ToLower() switch
            {
                "npgsql" or "postgresql" => AgentDataContext.CreateNpgsql(connectionString),
                "sqlite" => AgentDataContext.CreateSqlite(connectionString),
                "inmemory" => AgentDataContext.CreateInMemory(),
                _ => throw new NotSupportedException($"The provider '{provider}' is not supported. Supported providers are: 'npgsql', 'sqlite', 'inmemory'.")
            };
        }
    }
}
