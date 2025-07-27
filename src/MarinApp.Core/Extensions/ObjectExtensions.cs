using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MarinApp.Core.Extensions
{
    public static class ObjectExtensions
    {
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
