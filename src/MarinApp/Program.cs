using Luval.AuthMate.Infrastructure.Configuration;
using Luval.AuthMate.Web.Controllers;
using MarinApp.Components;
using MarinApp.Configuration;
using MarinApp.Core.Configuration;
using MarinApp.Core.Data;
using MarinApp.Core.Extensions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Diagnostics;

namespace MarinApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add the logging
            builder.AddApplicationLogging();

            // If debbugging add the debug level to debug
            if (Debugger.IsAttached)
                builder.Logging.SetMinimumLevel(LogLevel.Debug);

            // Add MudBlazor services
            builder.Services.AddMudServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Adds the configuration provider for the application
            builder.AddAppConfigurationProvider();

            // Add the database context
            builder.Services.AddDbContext<AppDataContext>(options =>
            {
                options.UseNpgsql(DbConnectionStringHelper.GetConnectionString())
                .LogTo(Console.WriteLine);
            });

            // Add the application OAuth Authentication
            builder.Services.AddApplicationAuth();

            // Add controllers, HTTP client, and context accessor
            // Add controller library assembly after AddControllers()
            builder.Services.AddControllers();

            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            /*** AuthMate: Additional configuration  ****/
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();
            app.MapControllers();
            /*** AuthMate:                           ****/

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
