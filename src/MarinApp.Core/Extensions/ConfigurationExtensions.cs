using MarinApp.Core.Configuration;
using MarinApp.Core.Data;
using MarinApp.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        public static IHostApplicationBuilder AddAppConfigurationProvider(this IHostApplicationBuilder builder)
        {
            // Setup the configuration data source
            var env = DbConnStrHelper.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            var connString = DbConnStrHelper.GetConnectionString();
            var serviceProvider = builder.Services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IDbContextFactory<AppDataContext>>();

            builder.Configuration.AddDbConfigurationProvider(
                factory,
                env);
            return builder;
        }
    }
}