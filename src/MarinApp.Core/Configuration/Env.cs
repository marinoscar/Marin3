using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Configuration
{
    public static class Env
    {
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
        public static string GetVariable(string name, string defaultValue = default!)
        {
            var result = Environment.GetEnvironmentVariable(name) ??
                   Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ??
                   Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);

            if (!string.IsNullOrEmpty(result)) return result;

            if (string.IsNullOrEmpty(defaultValue)) throw new ArgumentException($"Environment variable '{name}' is not set and no default value was provided.", nameof(name));
            else
                return defaultValue;
        }

        /// <summary>
        /// Sets the specified environment variable at the user level.
        /// </summary>
        /// <param name="name">The name of the environment variable to set.</param>
        /// <param name="value">The value to assign to the environment variable.</param>
        /// <remarks>
        /// This method sets the environment variable for the current user. 
        /// The change will persist for future processes started by the user.
        /// </remarks>
        public static void SetUserVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
        }

        /// <summary>
        /// Sets the specified environment variable at the machine level.
        /// </summary>
        /// <param name="name">The name of the environment variable to set.</param>
        /// <param name="value">The value to assign to the environment variable.</param>
        /// <remarks>
        /// This method sets the environment variable for the entire machine. 
        /// The change will persist for future processes started on the machine.
        /// Requires appropriate permissions.
        /// </remarks>
        public static void SetMachineVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Machine);
        }
    }
}
