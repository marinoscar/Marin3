using MarinApp.Expenses.Data;
using MarinApp.Expenses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarinApp.Expenses
{
    /// <summary>
    /// Service class for managing expense receipts and their line items.
    /// Provides methods for adding, updating, deleting, and retrieving receipts.
    /// </summary>
    public class ExpenseService
    {
        private readonly ExpenseDbContext _dbContext;
        private readonly ILogger<ExpenseService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpenseService"/> class.
        /// </summary>
        /// <param name="dbFactory">Factory to create <see cref="ExpenseDbContext"/> instances.</param>
        /// <param name="logger">Logger for logging errors and information.</param>
        /// <exception cref="ArgumentNullException">Thrown if dbFactory or logger is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the DbContext cannot be created.</exception>
        public ExpenseService(IDbContextFactory<ExpenseDbContext> dbFactory, ILogger<ExpenseService> logger)
        {
            if (dbFactory == null)
                throw new ArgumentNullException(nameof(dbFactory));

            // Create the database context using the provided factory.
            _dbContext = dbFactory.CreateDbContext()
                ?? throw new InvalidOperationException("Failed to create ExpenseDbContext from the provided factory.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Adds a new receipt or updates an existing one in the database.
        /// </summary>
        /// <param name="receipt">The receipt to add or update.</param>
        /// <returns>The added or updated receipt.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the receipt is null.</exception>
        /// <exception cref="Exception">Logs and rethrows any exception that occurs during the operation.</exception>
        public async Task<Receipt> AddOrUpdateReceipt(Receipt receipt)
        {
            if (receipt == null)
                throw new ArgumentNullException(nameof(receipt));

            try
            {
                // Attempt to find an existing receipt by its Id.
                var existingReceipt = await _dbContext.Receipts
                    .FirstOrDefaultAsync(r => r.Id == receipt.Id);

                if (existingReceipt == null)
                {
                    // If no existing receipt, set timestamps and initial version, then add to context.
                    receipt.UtcUpdatedAt = DateTime.UtcNow;
                    receipt.UtcCreatedAt = receipt.UtcCreatedAt;
                    receipt.Version = 1; // Set initial version for new receipts
                    await _dbContext.Receipts.AddAsync(receipt);
                }
                else
                {
                    // If receipt exists, update its values and increment version.
                    _dbContext.Entry(existingReceipt).CurrentValues.SetValues(receipt);
                    existingReceipt.UtcUpdatedAt = DateTime.UtcNow;
                    existingReceipt.Version++; // Increment version for updates
                }

                // Save changes to the database.
                await _dbContext.SaveChangesAsync();
                return receipt;
            }
            catch (Exception ex)
            {
                // Log any errors that occur and rethrow the exception.
                _logger.LogError(ex, "Error in AddOrUpdateReceipt for Receipt Id: {ReceiptId}", receipt.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a receipt and its associated line items from the database.
        /// </summary>
        /// <param name="id">The ID of the receipt to delete.</param>
        /// <returns>True if the receipt was deleted; false if not found.</returns>
        /// <exception cref="ArgumentException">Thrown if the receipt ID is null or empty.</exception>
        /// <exception cref="Exception">Logs and rethrows any exception that occurs during the operation.</exception>
        public async Task<bool> DeleteReceipt(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Receipt ID cannot be null or empty.", nameof(id));
            try
            {
                // Find the receipt by ID, including its line items.
                var receipt = await _dbContext.Receipts
                    .Include(r => r.LineItems)
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (receipt == null)
                {
                    // Log a warning if the receipt is not found.
                    _logger.LogWarning("Receipt with Id {ReceiptId} not found for deletion.", id);
                    return false; // Receipt not found
                }
                // Remove the receipt from the context.
                _dbContext.Receipts.Remove(receipt);
                // Save changes to the database.
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log any errors that occur and rethrow the exception.
                _logger.LogError(ex, "Error deleting Receipt with Id: {ReceiptId}", id);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a receipt by its ID, including its line items.
        /// </summary>
        /// <param name="id">The ID of the receipt to retrieve.</param>
        /// <returns>The receipt if found; otherwise, null.</returns>
        /// <exception cref="ArgumentException">Thrown if the receipt ID is null or empty.</exception>
        /// <exception cref="Exception">Logs and rethrows any exception that occurs during the operation.</exception>
        public async Task<Receipt?> GetReceiptById(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Receipt ID cannot be null or empty.", nameof(id));
            try
            {
                // Find the receipt by ID, including its line items.
                return await _dbContext.Receipts
                    .Include(r => r.LineItems)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                // Log any errors that occur and rethrow the exception.
                _logger.LogError(ex, "Error retrieving Receipt by Id: {ReceiptId}", id);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all receipts from the database, including their line items.
        /// </summary>
        /// <returns>A collection of all receipts.</returns>
        /// <exception cref="Exception">Logs and rethrows any exception that occurs during the operation.</exception>
        public async Task<IEnumerable<Receipt>> GetAllReceipts()
        {
            try
            {
                // Retrieve all receipts, including their line items.
                return await _dbContext.Receipts
                    .Include(r => r.LineItems)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log any errors that occur and rethrow the exception.
                _logger.LogError(ex, "Error retrieving all Receipts");
                throw;
            }
        }
    }
}
