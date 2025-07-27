using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MarinApp.Core.Extensions
{
    /// <summary>
    /// Provides extension methods for object serialization and manipulation.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Serializes the specified object to its JSON string representation.
        /// </summary>
        /// <param name="obj">The object to serialize. Must not be <c>null</c>.</param>
        /// <returns>
        /// A JSON-formatted string representing the serialized object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="obj"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when serialization fails for any reason.
        /// </exception>
        /// <remarks>
        /// The serialization uses <see cref="JsonSerializerOptions"/> with indented formatting
        /// and ignores object reference cycles to prevent serialization errors.
        /// </remarks>
        public static string ToJson(this object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };
                return JsonSerializer.Serialize(obj, obj.GetType(), options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to serialize object to JSON.", ex);
            }
        }
    }
}
