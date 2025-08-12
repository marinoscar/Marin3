using MarinApp.Expenses.Data;
using MarinApp.Expenses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Expenses
{
    public class ExpenseService
    {
        private readonly ExpenseDbContext _dbContext;
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(IDbContextFactory<ExpenseDbContext> dbFactory, ILogger<ExpenseService> logger)
        {
            if (dbFactory == null)
                throw new ArgumentNullException(nameof(dbFactory));

            _dbContext = dbFactory.CreateDbContext()
                ?? throw new InvalidOperationException("Failed to create ExpenseDbContext from the provided factory.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Receipt> AddOrUpdateReceipt(Receipt receipt)
        {
            if (receipt == null)
                throw new ArgumentNullException(nameof(receipt));

            try
            {
                var existingReceipt = await _dbContext.Receipts
                    .FirstOrDefaultAsync(r => r.Id == receipt.Id);

                if (existingReceipt == null)
                {
                    receipt.UtcUpdatedAt = DateTime.UtcNow;
                    receipt.UtcCreatedAt = receipt.UtcCreatedAt;
                    receipt.Version = 1; // Set initial version for new receipts
                    await _dbContext.Receipts.AddAsync(receipt);
                }
                else
                {
                    
                    _dbContext.Entry(existingReceipt).CurrentValues.SetValues(receipt);
                    existingReceipt.UtcUpdatedAt = DateTime.UtcNow;
                    existingReceipt.Version++; // Increment version for updates
                }

                await _dbContext.SaveChangesAsync();
                return receipt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddOrUpdateReceipt for Receipt Id: {ReceiptId}", receipt.Id);
                throw;
            }
        }

        public async Task<bool> DeleteReceipt(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Receipt ID cannot be null or empty.", nameof(id));
            try
            {
                var receipt = await _dbContext.Receipts
                    .Include(r => r.LineItems)
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (receipt == null)
                {
                    _logger.LogWarning("Receipt with Id {ReceiptId} not found for deletion.", id);
                    return false; // Receipt not found
                }
                _dbContext.Receipts.Remove(receipt);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Receipt with Id: {ReceiptId}", id);
                throw;
            }
        }

        public async Task<Receipt?> GetReceiptById(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Receipt ID cannot be null or empty.", nameof(id));
            try
            {
                return await _dbContext.Receipts
                    .Include(r => r.LineItems)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Receipt by Id: {ReceiptId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Receipt>> GetAllReceipts()
        {
            try
            {
                return await _dbContext.Receipts
                    .Include(r => r.LineItems)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all Receipts");
                throw;
            }
        }


    }
}
