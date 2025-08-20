using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Configuration
{
    /// <summary>
    /// Helper class for constructing PostgreSQL database connection strings using environment variables.
    /// </summary>
    /// <remarks>
    /// This class provides a method to build a PostgreSQL connection string by reading configuration values
    /// from environment variables. If an environment variable is not set, a default value is used.
    /// <list type="bullet">
    /// <item><description><c>DB_SERVER</c>: The database server host (default: "localhost").</description></item>
    /// <item><description><c>DB_PORT</c>: The database server port (default: "5432").</description></item>
    /// <item><description><c>DB_USER</c>: The database user name (default: "admin").</description></item>
    /// <item><description><c>DB_PASSWORD</c>: The database user password (default: "your_password").</description></item>
    /// </list>
    /// </remarks>
    public class DbConnectionStringHelper
    {
        /// <summary>
        /// Gets the PostgreSQL connection string for the specified database.
        /// </summary>
        /// <param name="dbName">The name of the database to connect to.</param>
        /// <returns>
        /// A PostgreSQL connection string constructed from environment variables and the provided database name.
        /// </returns>
        /// <example>
        /// <code>
        /// string connStr = DbConnectionStringHelper.GetConnectionString("MyDatabase");
        /// </code>
        /// </example>
        /// <remarks>
        /// The following environment variables are used (with defaults if not set):
        /// <list type="bullet">
        /// <item><description><c>DB_SERVER</c>: Database server host (default: "localhost")</description></item>
        /// <item><description><c>DB_PORT</c>: Database server port (default: "5432")</description></item>
        /// <item><description><c>DB_USER</c>: Database user (default: "admin")</description></item>
        /// <item><description><c>DB_PASSWORD</c>: Database password (default: "your_password")</description></item>
        /// </list>
        /// </remarks>
        public static string GetConnectionString(string dbName = "marinapp")
        {
            var port = GetEnvironmentVariable("DB_PORT", "5432");

            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = GetEnvironmentVariable("DB_SERVER", "localhost"),
                Port = int.Parse(port),
                Database = dbName,
                Username = GetEnvironmentVariable("DB_USER", "admin"),
                Password = GetEnvironmentVariable("DB_PASSWORD", "your_password"),
            };
            return builder.ConnectionString;
        }

        /// <summary>
        /// Retrieves the value of an environment variable by searching in multiple scopes.
        /// </summary>
        /// <param name="name">
        /// The name of the environment variable to retrieve.  
        /// This parameter must not be <c>null</c> or empty.
        /// </param>
        /// <param name="defaultValue">
        /// The value to return if the environment variable is not found in any scope.  
        /// Defaults to <c>default!</c>, which is <c>null</c> for reference types unless explicitly provided.
        /// </param>
        /// <returns>
        /// The value of the environment variable if found; otherwise, the <paramref name="defaultValue"/>.  
        /// The method checks the following scopes in order:
        /// <list type="number">
        ///   <item><description>Process-level environment variables</description></item>
        ///   <item><description>User-level environment variables</description></item>
        ///   <item><description>Machine-level (system-wide) environment variables</description></item>
        /// </list>
        /// If none of these contain the variable, <paramref name="defaultValue"/> is returned.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is useful when you want to provide a fallback for configuration values
        /// that may be set in different scopes (process, user, or machine).
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// string connectionString = GetEnvironmentVariable("DB_CONNECTION", "Server=localhost;Database=myDb;");
        /// </code>
        /// </para>
        /// </remarks>
        public static string? GetEnvironmentVariable(string name, string defaultValue = default!)
        {
            return Environment.GetEnvironmentVariable(name) ?? 
                   Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ?? 
                   Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine) ??
                   defaultValue;
        }

    }
}
