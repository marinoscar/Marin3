using MarinApp.Expenses.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarinApp.Expenses
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds the MarinApp Expenses services to the specified service collection.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddExpensesApp(this IServiceCollection services, string connectionString)
        {
            // Register the DbContext factory for ExpenseDbContext
            services.AddDbContextFactory<ExpenseDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });
            // Register the ExpenseService
            services.AddScoped<ExpenseService>();
            return services;
        }
    }
}
