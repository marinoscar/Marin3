using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Configuration
{
    /// <summary>
    /// Represents the application context configuration, providing metadata and environment information
    /// for the running instance of the MarinApp application.
    /// </summary>
    /// <remarks>
    /// This class is typically used to store and access application-level settings such as
    /// identification, versioning, environment, and descriptive information. It can be injected
    /// or accessed throughout the application to provide context-aware behavior.
    /// </remarks>
    public class AppContext
    {
        /// <summary>
        /// Gets or sets the unique identifier for the application instance.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> representing the application's unique ID.
        /// </value>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> representing the application's name.
        /// </value>
        public string AppName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version of the application.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> representing the application's version.
        /// </value>
        public string AppVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the environment in which the application is running.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> representing the environment name (e.g., "Development", "Production").
        /// Defaults to "Development".
        /// </value>
        public string Environment { get; set; } = "Development";

        /// <summary>
        /// Gets or sets a description of the application.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> providing additional descriptive information about the application.
        /// </value>
        public string Description { get; set; } = string.Empty;
    }
}
