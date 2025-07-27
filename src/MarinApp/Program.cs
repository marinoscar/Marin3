using MarinApp.Components;
using MarinApp.Configuration;
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

            // Add the logging
            builder.AddApplicationLogging();

            // Add MudBlazor services
            builder.Services.AddMudServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Setup the configuration data source
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var connString = DbConnectionStringHelper.GetConnectionString();
            builder.Configuration.AddDbConfigurationProvider(
                options => options.UseNpgsql(connString).LogTo(Console.WriteLine),
                env);

            // Add the database context
            builder.Services.AddDbContext<AppDataContext>(options =>
            {
                options.UseNpgsql(connString)
                .LogTo(Console.WriteLine);
            });

            // Add the application OAuth Authentication
            builder.Services.AddApplicationAuth();

            // Add controllers, HTTP client, and context accessor
            builder.Services.AddControllers();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();


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
