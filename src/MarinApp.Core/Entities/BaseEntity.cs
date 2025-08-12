using MarinApp.Core.Data;
using MarinApp.Core.Extensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Entities
{
    /// <summary>
    /// Represents the base entity for all domain entities in the application.
    /// Provides common properties such as Id, creation and update timestamps, and versioning for concurrency control.
    /// </summary>
    public class BaseEntity
    {
        /// <summary>
        /// Gets or sets the unique string identifier for the entity.
        /// This value is generated as a new GUID, formatted as an uppercase string without dashes.
        /// </summary>
        [Key]
        [Display(Name = "Id", Description = "A unique string identifier for the entity.")]
        public string Id { get; set; } = Guid.NewGuid().ToString().Replace("-", "").ToUpperInvariant();

        /// <summary>
        /// Gets or sets the UTC date and time when the entity was created.
        /// This value is set to the current UTC time upon entity instantiation.
        /// </summary>
        [Required]
        [Display(Name = "Created At (UTC)", Description = "The date and time when the entity was created, in UTC.")]
        public DateTime UtcCreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the UTC date and time when the entity was last updated.
        /// This value is set to the current UTC time upon entity instantiation and should be updated on modifications.
        /// </summary>
        [Required]
        [Display(Name = "Updated At (UTC)", Description = "The date and time when the entity was last updated, in UTC.")]
        public DateTime UtcUpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the version number of the entity for concurrency control.
        /// This value is incremented with each update to the entity.
        /// </summary>
        [Required]
        [Display(Name = "Version", Description = "The version number of the entity for concurrency control.")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Returns a JSON string representation of the entity.
        /// </summary>
        /// <returns>A JSON-formatted string representing the current entity instance.</returns>
        public override string ToString()
        {
            return this.ToJson();
        }

        /// <summary>
        /// Retrieves the UI metadata for the current entity type.
        /// This metadata includes information such as the entity's name, display name, description,
        /// and a collection of field metadata describing each property of the entity.
        /// <para>
        /// The <see cref="UIEntityMetadata"/> object returned by this method is typically used to
        /// dynamically generate user interface forms or views based on the structure and attributes
        /// of the entity. It leverages reflection and custom attributes (such as <see cref="DisplayAttribute"/>,
        /// <see cref="KeyAttribute"/>, and <see cref="RequiredAttribute"/>) to extract descriptive and
        /// structural information about the entity's properties.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var metadata = myEntity.GetMetadata();
        /// foreach (var field in metadata.Fields)
        /// {
        ///     Console.WriteLine($"{field.Name}: {field.DisplayName} ({field.FieldType.Name})");
        /// }
        /// </code>
        /// </para>
        /// </summary>
        /// <returns>
        /// A <see cref="UIEntityMetadata"/> instance containing metadata for the current entity type,
        /// including its name, display name, description, and field definitions.
        /// </returns>
        public UIEntityMetadata GetMetadata()
        {
            return UIMetadataExtractor.Get(this.GetType());
        }

    }
}
