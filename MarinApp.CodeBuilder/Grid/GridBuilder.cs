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

        public string CreateGrid(string componentName, AssemblyName assemblyName, string dbContextName, string entityTypeName)
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

            return CreateGrid(componentName, dbContextType,  dbSet);
        }

        private string CreateGrid(string componentName, Type dbContextType, PropertyInfo propertyInfo)
        {
            var entityType = propertyInfo.PropertyType.GetGenericArguments()[0];
            var metadata = UIMetadataExtractor.Get(entityType);
            var sw = new StringWriter();

            sw.WriteLine("@using Microsoft.EntityFrameworkCore");
            sw.WriteLine("@using ", dbContextType.Namespace);
            if(dbContextType.Namespace != entityType.Namespace)
                sw.WriteLine("@using ", entityType.Namespace);

            sw.WriteLine("@page /", componentName.ToLower());
            sw.WriteLine("");
            sw.WriteLine("@inject IDbContextFactory<{0}> ContextFactory", dbContextType.Name);
            sw.WriteLine("@inject ILogger<{0}> Logger", componentName);
            sw.WriteLine("");
            sw.WriteLine("<MudDataGrid T=\"{0}\"", entityType.Name);
            sw.WriteLine("             Items=\"@Data\"");
            sw.WriteLine("             ReadOnly=\"false\"");
            sw.WriteLine("  <ToolBarContent>");
            sw.WriteLine("      <MudText Typo=\"Typo.h3\">{0}</MudText>", metadata.DisplayName);
            sw.WriteLine("      <MudSpacer />");
            sw.WriteLine("  </ToolBarContent>");
            sw.WriteLine("  <Columns>");
            foreach(var field in metadata.Fields.Where(i => !i.IsPrimaryKey))
            {
                sw.WriteLine("      <PropertyColumn Property=\"x => x.{0}\" Title=\"{1}\" Sortable=\"true\" Filterable=\"true\" />", field.Name, field.DisplayName);
            }
            sw.WriteLine("  </Columns>");
            sw.WriteLine("</MudDataGrid>");
            sw.WriteLine("");
            sw.WriteLine("@code {");
            sw.WriteLine("");
            sw.WriteLine("  private {0} Context => ContextFactory.CreateDbContext();", dbContextType.Name);
            sw.WriteLine("  public IEnumerable<{0}> Data { get; set; }", entityType.Name);
            sw.WriteLine("");
            sw.WriteLine("  protected override async Task OnInitializedAsync()");
            sw.WriteLine("  {");
            sw.WriteLine("      Data = await Context.{0}", propertyInfo.Name);
            sw.WriteLine("          .AsNoTracking()");
            sw.WriteLine("          .ToListAsync(CancellationToken.None);");
            sw.WriteLine("      if (Data == null || !Data.Any())");
            sw.WriteLine("          Logger.LogWarning(\"No data found, initializing with empty list.\");");
            sw.WriteLine("");
            sw.WriteLine("      await base.OnInitializedAsync();");
            sw.WriteLine("  }");
            sw.WriteLine("}");
            return sw.ToString();
        }


    }
}
