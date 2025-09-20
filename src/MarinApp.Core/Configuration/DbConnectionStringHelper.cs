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


        ///<summary>
        /// Retrieves the value of the specified environment variable, searching in the process, user, and machine scopes.
        /// If the environment variable is not set in any scope, returns the provided default value.
        /// </summary>
        /// <param name="name">The name of the environment variable to retrieve.</param>
        /// <param name="defaultValue">
        /// The default value to return if the environment variable is not set in any scope.
        /// If not provided and the variable is not set, an <see cref="ArgumentException"/> is thrown.
        /// </param>
        /// <returns>
        /// The value of the environment variable if found; otherwise, the <paramref name="defaultValue"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the environment variable is not set in any scope and <paramref name="defaultValue"/> is <c>null</c> or empty.
        /// </exception>
        /// <remarks>
        /// This method checks for the environment variable in the following order:
        /// <list type="number">
        /// <item>Process-level environment variables.</item>
        /// <item>User-level environment variables.</item>
        /// <item>Machine-level environment variables.</item>
        /// </list>
        /// If the variable is not found in any scope, the <paramref name="defaultValue"/> is returned.
        /// If <paramref name="defaultValue"/> is not provided, an exception is thrown.
        /// </remarks>
        public static string? GetEnvironmentVariable(string name, string defaultValue = default!)
        {
            var result = Environment.GetEnvironmentVariable(name) ??
                   Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ??
                   Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);

            if (!string.IsNullOrEmpty(result)) return result;

            if (string.IsNullOrEmpty(defaultValue)) throw new ArgumentException($"Environment variable '{name}' is not set and no default value was provided.", nameof(name));
            else
                return defaultValue;
        }

    }
}
