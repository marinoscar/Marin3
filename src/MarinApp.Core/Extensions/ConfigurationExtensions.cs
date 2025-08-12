using MarinApp.Core.Configuration;
using MarinApp.Core.Data;
using MarinApp.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Extensions
{
    
    public static class ConfigurationExtensions
    {
        
        public static IConfigurationBuilder AddDbConfigurationProvider(
            this IConfigurationBuilder builder,
             IDbContextFactory<Data.AppDataContext> factory,
            string environment)
        {

            return builder.Add(new DbConfigurationSource(factory, environment));
        }


        public static IServiceCollection AddApplicationCoreServices(this IServiceCollection s)
        {
            s.AddScoped<AppConfigurationService>();

            //this to be the last call, it will load all of the metadata for all data context types
            s.LoadUIMetadataForAllDataContext();
            return s;
        }
    }
}
