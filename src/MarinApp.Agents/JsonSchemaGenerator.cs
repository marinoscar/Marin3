using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MarinApp.Agents
{

    public static class JsonSchemaGenerator
    {
        /// <summary>
        /// Generate a JSON Schema (Draft-07-ish) for T and return it as a JsonDocument.
        /// </summary>
        public static JsonDocument Generate<T>(bool additionalProperties = false)
            => Generate(typeof(T), additionalProperties);

        /// <summary>
        /// Generate a JSON Schema (Draft-07-ish) for the provided type and return it as a JsonDocument.
        /// </summary>
        public static JsonDocument Generate(Type type, bool additionalProperties = false)
        {
            var visited = new Dictionary<Type, string>(); // type -> #/definitions/Name
            var definitions = new JsonObject();

            JsonNode root = BuildSchema(type, visited, definitions);
            var obj = new JsonObject
            {
                ["$schema"] = "http://json-schema.org/draft-07/schema#",
                ["type"] = "object",
                ["additionalProperties"] = additionalProperties,
                ["properties"] = root["properties"] ?? new JsonObject()
            };

            if (root["required"] is JsonArray req) obj["required"] = req;
            if (definitions.Count > 0) obj["definitions"] = definitions;

            return JsonDocument.Parse(obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private static JsonNode BuildSchema(
            Type type,
            Dictionary<Type, string> visited,
            JsonObject definitions)
        {
            // Complex object => describe properties
            var propsObj = new JsonObject();
            var required = new JsonArray();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetMethod == null) continue;

                var (schema, isNullable) = DescribeType(prop.PropertyType, visited, definitions);

                // Description from [Description]
                if (prop.GetCustomAttribute<DescriptionAttribute>() is { } da)
                {
                    EnsureObject(schema)["description"] = da.Description;
                }

                // String length constraints
                if (prop.GetCustomAttribute<MaxLengthAttribute>() is { } max)
                    EnsureObject(schema)["maxLength"] = max.Length;
                if (prop.GetCustomAttribute<MinLengthAttribute>() is { } min)
                    EnsureObject(schema)["minLength"] = min.Length;

                // Range constraints (numeric)
                if (prop.GetCustomAttribute<RangeAttribute>() is { } range)
                {
                    if (double.TryParse(range.Minimum?.ToString(), out var minVal))
                        EnsureObject(schema)["minimum"] = minVal;
                    if (double.TryParse(range.Maximum?.ToString(), out var maxVal))
                        EnsureObject(schema)["maximum"] = maxVal;
                }

                // Required?
                bool hasRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                if (hasRequired && !isNullable)
                    required.Add(prop.Name);

                propsObj[prop.Name] = schema;
            }

            var obj = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = propsObj
            };
            if (required.Count > 0) obj["required"] = required;
            return obj;
        }

        private static (JsonNode Schema, bool IsNullable) DescribeType(
            Type t,
            Dictionary<Type, string> visited,
            JsonObject definitions)
        {
            // Nullable<T> or reference types
            bool isNullable = IsNullableType(t, out var inner);
            t = inner ?? t;

            // Primitives & well-known
            if (t == typeof(string)) return (TypeNode("string"), isNullable);
            if (t == typeof(bool)) return (TypeNode("boolean"), isNullable);
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) return (TypeNode("integer"), isNullable);
            if (t == typeof(float) || t == typeof(double) || t == typeof(decimal)) return (TypeNode("number"), isNullable);
            if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
            {
                var n = TypeNode("string");
                EnsureObject(n)["format"] = "date-time";
                return (n, isNullable);
            }
            if (t == typeof(Guid))
            {
                var n = TypeNode("string");
                EnsureObject(n)["format"] = "uuid";
                return (n, isNullable);
            }

            // Enum → string with enum values (or use integer by changing here)
            if (t.IsEnum)
            {
                var n = TypeNode("string");
                var values = new JsonArray();
                foreach (var name in Enum.GetNames(t)) values.Add(name);
                EnsureObject(n)["enum"] = values;
                return (n, isNullable);
            }

            // IEnumerable<T> → array
            if (ImplementsGeneric(t, typeof(IEnumerable<>), out var enumerableArg) && t != typeof(string))
            {
                var (itemSchema, _) = DescribeType(enumerableArg!, visited, definitions);
                var arr = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = itemSchema
                };
                return (arr, isNullable);
            }

            // Complex object → definitions + $ref
            if (!visited.TryGetValue(t, out var defRef))
            {
                defRef = $"#/definitions/{t.Name}";
                visited[t] = defRef;

                // build and stash definition
                var def = BuildSchema(t, visited, definitions);
                definitions[t.Name] = def;
            }

            var refNode = new JsonObject { ["$ref"] = defRef };
            return (refNode, isNullable);
        }

        // Helpers

        private static JsonNode TypeNode(string type) => new JsonObject { ["type"] = type };

        private static JsonObject EnsureObject(JsonNode node) =>
            node as JsonObject ?? throw new InvalidOperationException("Expected JsonObject");

        private static bool IsNullableType(Type t, out Type? inner)
        {
            inner = null;

            // Nullable<T>
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                inner = t.GetGenericArguments()[0];
                return true;
            }

            // Reference type (treat as nullable for "required" purposes)
            if (!t.IsValueType)
            {
                inner = t;
                return true;
            }

            return false;
        }

        private static bool ImplementsGeneric(Type t, Type openGeneric, out Type? arg)
        {
            arg = null;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric)
            {
                arg = t.GetGenericArguments().FirstOrDefault();
                return true;
            }

            var iface = t.GetInterfaces()
                         .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
            if (iface != null)
            {
                arg = iface.GetGenericArguments()[0];
                return true;
            }
            return false;
        }
    }

}
