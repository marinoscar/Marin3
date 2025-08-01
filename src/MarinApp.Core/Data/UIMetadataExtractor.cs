using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Data
{
    public class UIMetadataExtractor
    {
        private static readonly Dictionary<Type, UIEntityMetadata> _metadata = [];

        public void InitializeDataContext<Context>() where Context : DbContext
        {
            InitializeDataContext(typeof(Context));
        }

        public void InitializeDataContext(Type contextType)
        {
            if (!typeof(DbContext).IsAssignableFrom(contextType))
                throw new ArgumentException($"Type '{contextType.FullName}' is not a DbContext or does not inherit from DbContext.", nameof(contextType));


            var dbSetProp = contextType.GetProperties()
            .Where(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(i => i.PropertyType.GenericTypeArguments.FirstOrDefault())
            .ToList();
            foreach (var type in dbSetProp)
            {
                if (type != null && !_metadata.ContainsKey(type))
                {
                    _metadata[type] = Extract(type);
                }
            }
        }

        public UIEntityMetadata Get<T>()
        {
            return Get(typeof(T));
        }

        public UIEntityMetadata Get(Type type)
        {
            if (_metadata.ContainsKey(type)) return _metadata[type];
            _metadata[type] = Extract(type);
            return _metadata[type];
        }

        private UIEntityMetadata Extract(Type type)
        {
            var prop = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && !p.GetIndexParameters().Any())
                .ToList();
            var fielsds = prop.Select(ExtractField).ToList();

            return new UIEntityMetadata
            {
                Name = type.Name,
                DisplayName = GetDisplayName(type),
                Description = GetDescription(type),
                Fields = fielsds
            };
        }

        private UIFieldMetadata ExtractField(PropertyInfo property)
        {
            var result = new UIFieldMetadata
            {
                Name = property.Name,
                DisplayName = GetDisplayName(property),
                Description = GetDescription(property),
                IsForeignKey = property.GetCustomAttribute<ForeignKeyAttribute>() != null,
                IsRequired = property.GetCustomAttribute<RequiredAttribute>() != null,
                MaxLength = GetMaxLength(property)
            };
            return result;
        }

        private string GetDisplayName(MemberInfo m)
        {
            return Convert.ToString(GetAttributeValue<DisplayAttribute>(m, a => a.Name)) ?? m.Name;
        }

        private string? GetDescription(MemberInfo m)
        {
            return Convert.ToString(GetAttributeValue<DisplayAttribute>(m, a => a.Description));
        }

        private int GetMaxLength(MemberInfo m)
        {
            return Convert.ToInt32(GetAttributeValue<MaxLengthAttribute>(m, a => a.Length) ?? 0);
        }

        private object? GetAttributeValue<T>(MemberInfo m, Func<T, object> valueSelector) where T : Attribute
        {
            var attribute = m.GetCustomAttribute<T>();
            return attribute != null ? valueSelector(attribute) : string.Empty;
        }

    }

    public static class UIMetadataExtractorExtensions
    {
        /// <summary>
        /// Scans the registered <see cref="DbContext"/> and <see cref="IDbContextFactory{TContext}"/> services in the <see cref="IServiceCollection"/>,
        /// extracts UI metadata for all discovered data context types, and initializes the metadata extractor for each.
        /// </summary>
        /// <param name="s">The <see cref="IServiceCollection"/> containing registered services.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="s"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no <see cref="DbContext"/> or <see cref="IDbContextFactory{TContext}"/> types are found in the service collection.</exception>
        public static IServiceCollection LoadUIMetadataForAllDataContext(this IServiceCollection s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), "The service collection cannot be null.");

            var metadataExtractor = new UIMetadataExtractor();

            var dbContextTypes = s
                .Where(sd =>
                    typeof(DbContext).IsAssignableFrom(sd.ServiceType) &&
                    sd.ServiceType != typeof(DbContext))
                .Select(sd => sd.ServiceType)
                .Distinct();

            var factoryTypes = s
                .Where(sd =>
                    sd.ServiceType.IsGenericType &&
                    sd.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextFactory<>))
                .Select(sd => sd.ServiceType.GenericTypeArguments[0])
                .Where(t => typeof(DbContext).IsAssignableFrom(t) && t != typeof(DbContext));

            var allTypes = dbContextTypes.Concat(factoryTypes).Distinct().ToList();

            // Initialize metadata for each DbContext type
            foreach (var type in allTypes)
            {
                try
                {
                    metadataExtractor.InitializeDataContext(type);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to initialize UI metadata for DbContext type '{type.FullName}'.", ex);
                }
            }

            return s;
        }
    }
}
