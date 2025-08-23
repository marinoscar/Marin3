using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents.Data
{
    public class AgentDataContext : DbContext
    {
        public AgentDataContext(DbContextOptions<AgentDataContext> options) : base(options)
        {
        }
        public DbSet<AgentMessage> AgentMessages { get; set; } = default!;
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
                      .HasColumnType("jsonb")
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
        public static AgentDataContext CreateSqlite(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");

            var optionsBuilder = new DbContextOptionsBuilder<AgentDataContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new AgentDataContext(optionsBuilder.Options);
        }


    }
}
