using MarinApp.Core.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.CodeBuilder.Grid
{
    public class GridBuilder
    {

        public string CreateGrid(AssemblyName assemblyName, string dbContextName, string entityTypeName)
        {
            // 1. Load assembly
            var assembly = Assembly.Load(assemblyName);

            // 2. Find DbContext type by name (case-insensitive)
            var dbContextType = assembly.GetTypes()
                .FirstOrDefault(t => string.Equals(t.Name, dbContextName, StringComparison.OrdinalIgnoreCase));

            if (dbContextType == null)
                throw new Exception($"DbContext class '{dbContextName}' not found in assembly '{assemblyName.Name}'.");

            // 3. Check it inherits from DbContext
            if (!typeof(DbContext).IsAssignableFrom(dbContextType))
                throw new Exception($"Type '{dbContextName}' does not inherit from DbContext.");

            // 4. Find entity type by name (case-insensitive)
            var entityType = assembly.GetTypes()
                .FirstOrDefault(t => string.Equals(t.Name, entityTypeName, StringComparison.OrdinalIgnoreCase));

            if (entityType == null)
                throw new Exception($"Entity type '{entityTypeName}' not found in assembly '{assemblyName.Name}'.");

            // 5. Look for DbSet<TEntity> property (case-insensitive property names)
            var dbSet = dbContextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.PropertyType.IsGenericType
                          && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
                          && string.Equals(p.PropertyType.GetGenericArguments()[0].Name, entityType.Name, StringComparison.OrdinalIgnoreCase));

            if (dbSet == null)
                throw new Exception($"DbContext '{dbContextName}' does not have a DbSet<{entityTypeName}> property.");

            //Loads the metadata for the DbContext type
            UIMetadataExtractor.InitializeDataContext(dbContextType);

            return CreateGrid(dbContextType,  dbSet);
        }

        private string CreateGrid(Type dbContextType, PropertyInfo propertyInfo)
        {
            var entityType = propertyInfo.PropertyType.GetGenericArguments()[0];
            var metadata = UIMetadataExtractor.Get(entityType);
            var sw = new StringWriter();
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            sw.WriteLine("");
            return string.Empty;
        }


    }
}
