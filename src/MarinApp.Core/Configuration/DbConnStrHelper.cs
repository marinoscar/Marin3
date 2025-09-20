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
    public class DbConnStrHelper
    {

        public const string EnvVariableDbServer = "MARIN_APP_DB_SERVER";
        public const string EnvVariableDbPort = "MARIN_APP_DB_PORT";
        public const string EnvVariableDbUser = "MARIN_APP_DB_USER";
        public const string EnvVariableDbPassword = "MARIN_APP_DB_PASSWORD";
        public const string EnvVariableDbName = "MARIN_APP_DB_NAME";


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
        public static string GetConnectionString(string dbName)
        {
            var port = Env.GetVariable(EnvVariableDbPort, "5432");

            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = Env.GetVariable(EnvVariableDbServer),
                Port = int.Parse(port),
                Database = Env.GetVariable(EnvVariableDbName, "marinapp"),
                Username = Env.GetVariable(EnvVariableDbUser),
                Password = Env.GetVariable(EnvVariableDbPassword),
            };
            return builder.ConnectionString;
        }
    }
}
