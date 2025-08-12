using MarinApp.Expenses.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Expenses.Data
{
    /// <summary>
    /// Represents the Entity Framework Core database context for managing receipts and line items.
    /// Includes DbSet properties for <see cref="Receipt"/> and <see cref="LineItem"/>, and is configured
    /// to work with a PostgreSQL database provider.
    /// </summary>
    /// <remarks>
    /// Provides the implementation of a <see cref="DbContext"/> 
    /// </remarks>
    public class ExpenseDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpenseDbContext"/> class using the specified options.
        /// </summary>
        /// <param name="options">The options to be used by the DbContext.</param>
        public ExpenseDbContext(DbContextOptions<ExpenseDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the database table for expense receipts.
        /// </summary>
        public DbSet<Receipt> Receipts { get; set; } = default!;

        /// <summary>
        /// Gets or sets the database table for line items associated with receipts.
        /// </summary>
        public DbSet<LineItem> LineItems { get; set; } = default!;
    }
}
