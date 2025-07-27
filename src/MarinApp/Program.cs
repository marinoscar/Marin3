using MarinApp.Components;
using MarinApp.Core.Configuration;
using MarinApp.Core.Data;
using MarinApp.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

namespace MarinApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add MudBlazor services
            builder.Services.AddMudServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Setup the configuration data source
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var connString = DbConnectionStringHelper.GetConnectionString("marinapp");
            builder.Configuration.AddDbConfigurationProvider(
                options => options.UseNpgsql(connString),
                env);

            // Add the database context
            builder.Services.AddDbContext<AppDataContext>(options =>
            {
                options.UseNpgsql(connString);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
