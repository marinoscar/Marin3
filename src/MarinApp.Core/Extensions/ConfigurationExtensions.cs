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
    /// <summary>
    /// Provides extension methods for <see cref="IConfigurationBuilder"/> to support
    /// loading configuration values from a database using a custom configuration provider.
    /// </summary>
    public static class ConfigurationExtensions
    {
        
        public static IConfigurationBuilder AddDbConfigurationProvider(
            this IConfigurationBuilder builder,
            AppDataContext dataContext,
            string environment)
        {

            return builder.Add(new DbConfigurationSource(dataContext, environment));
        }


        public static IServiceCollection AddApplicationCoreServices(this IServiceCollection s)
        {
            s.AddScoped<AppConfigurationService>();
            return s;
        }
    }
}
