using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Entities
{
    /// <summary>
    /// Represents an application configuration setting, including its key, value, and optional environment.
    /// </summary>
    /// <remarks>
    /// Provides an implementation of the base class <see cref="BaseEntity"/> with properties for managing configuration settings.
    /// The property <see cref="Key"/> is indexed to ensure uniqueness across configuration settings.
    /// </remarks>
    [Index(nameof(Key), Name = "IX_Unique_" + nameof(Key), IsUnique = true)]
    [Display(Name = "Application Configuration", Description = "Represents a configuration setting for the application with an associated key, value, and optional environment.")]
    public class AppConfiguration : BaseEntity
    {
        /// <summary>
        /// Gets or sets the key that uniquely identifies the configuration setting.
        /// </summary>
        [Required]
        [Display(Name = "Configuration Key", Description = "The unique key that identifies the configuration setting.")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value associated with the configuration key.
        /// </summary>
        [Required]
        [Display(Name = "Configuration Value", Description = "The value associated with the configuration key.")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the environment for which this configuration is applicable. Can be null if not environment-specific.
        /// </summary>
        [Display(Name = "Environment", Description = "The environment (e.g., Development, Staging, Production) where the configuration applies. Can be null.")]
        public string? Environment { get; set; }
    }
}
