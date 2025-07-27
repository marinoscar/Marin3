using MarinApp.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Configuration
{
    /// <summary>
    /// Provides configuration values from a database source using Entity Framework Core.
    /// This provider loads configuration entries from the <see cref="AppConfiguration"/> table,
    /// supporting both shared (environment-agnostic) and environment-specific settings.
    /// Environment-specific settings override shared settings with the same key.
    /// </summary>
    public class DbConfigurationProvider : ConfigurationProvider
    {
        
        private readonly AppDataContext _dataContext;
        private readonly string _environment;

       
        public DbConfigurationProvider(AppDataContext dataContext, string environment)
        {
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public override void Load()
        {
            
            // Ensure the configuration table exists; creates it if it does not (using EF migrations if available).
            _dataContext.Database.EnsureCreated();

            // Retrieve configuration entries: shared (Environment == null) and environment-specific.
            // Environment-specific entries override shared ones with the same key.
            var configEntries = _dataContext.AppConfiguration
                .Where(e => e.Environment == null || e.Environment == _environment)
                .OrderBy(e => e.Environment == null ? 0 : 1) // Shared first, then environment-specific.
                .ToList();

            var data = new Dictionary<string, string>();
            foreach (var entry in configEntries)
            {
                // Environment-specific entries will overwrite shared ones with the same key.
                data[entry.Key] = entry.Value ?? string.Empty;
            }

            Data = data;
        }
    }
}
