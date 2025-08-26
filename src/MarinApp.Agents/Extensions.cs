using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MarinApp.Agents
{
    /// <summary>
    /// Provides extension methods for object serialization and schema generation.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Generates a JSON Schema <see cref="JsonDocument"/> for the specified object's type.
        /// </summary>
        /// <param name="obj">
        /// The object whose type will be used to generate the JSON Schema.
        /// </param>
        /// <param name="additionalProperties">
        /// Indicates whether the generated schema should allow additional properties not explicitly defined.
        /// Defaults to <c>false</c>.
        /// </param>
        /// <returns>
        /// A <see cref="JsonDocument"/> representing the JSON Schema of the object's type,
        /// or <c>null</c> if <paramref name="obj"/> is <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method uses <see cref="JsonSchemaGenerator.Generate(Type, bool)"/> to create the schema.
        /// The schema can be used for validation, documentation, or integration with tools that consume JSON Schema.
        /// </remarks>
        /// <example>
        /// <code>
        /// var person = new Person { Name = "Alice", Age = 30 };
        /// JsonDocument schema = person.ToJsonSchema();
        /// </code>
        /// </example>
        public static JsonDocument ToJsonSchema(this object obj, bool additionalProperties = false)
        {
            if (obj == null) return default(JsonDocument)!;
            return JsonSchemaGenerator.Generate(obj.GetType(), additionalProperties);
        }
    }
}
