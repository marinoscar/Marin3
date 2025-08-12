using MarinApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MarinApp.Expenses.Entities
{
    /// <summary>
    /// Represents a receipt for an expense transaction, capturing essential information about the purchase,
    /// including vendor details, transaction date, total amount, currency used, and associated line items.
    /// Inherits audit metadata and identity fields from <see cref="BaseEntity"/>.
    /// </summary>
    /// <remarks>
    /// Provides an implementation of the base class located here: https://gist.github.com/marinoscar/6e9b0a0e47004428ca48354594c69dd5
    /// </remarks>
    [Display(
        Name = "Expense Receipt",
        Description = "Represents a receipt for an expense transaction including vendor, date, total, and associated line items."
    )]
    [Index(nameof(Vendor), nameof(Date), IsUnique = true, Name = "IX_Receipt_Vendor_Date")]
    public class Receipt : BaseEntity
    {
        /// <summary>
        /// Gets or sets the date when the purchase was made.
        /// This field is required and part of a unique constraint with the vendor.
        /// </summary>
        [Required]
        [Display(Name = "Date of Purchase", Description = "The date when the purchase occurred.")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the name of the vendor or store where the purchase was made.
        /// This field is required, limited to 200 characters, and part of a unique constraint with the date.
        /// </summary>
        [Required]
        [StringLength(200)]
        [Display(Name = "Vendor", Description = "The vendor or store where the purchase was made.")]
        public string Vendor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total amount paid for the receipt.
        /// The amount is stored as a numeric(18,2), compatible with PostgreSQL.
        /// </summary>
        [Required]
        [Column(TypeName = "numeric(18,2)")]
        [Display(Name = "Total Amount", Description = "The total amount paid on the receipt.")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the ISO currency code (e.g., USD, EUR) of the transaction.
        /// Limited to 3 characters.
        /// </summary>
        [StringLength(3)]
        [Display(Name = "Currency", Description = "Currency of the transaction, e.g. USD")]
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Gets or sets the collection of line items associated with this receipt.
        /// Each line item represents a breakdown of individual purchases included in the receipt.
        /// </summary>
        [Display(Name = "Line Items", Description = "The individual line items on the receipt.")]
        public virtual ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();
    }

    /// <summary>
    /// Represents a single line item associated with a receipt, including item description,
    /// quantity, price per unit, and a foreign key relationship to the parent receipt.
    /// Inherits audit metadata and identity fields from <see cref="BaseEntity"/>.
    /// </summary>
    /// <remarks>
    /// Provides an implementation of the base class located here: https://gist.github.com/marinoscar/6e9b0a0e47004428ca48354594c69dd5
    /// </remarks>
    [Display(
        Name = "Receipt Line Item",
        Description = "Represents an individual item in a receipt, including quantity and unit price."
    )]
    public class LineItem : BaseEntity
    {
        /// <summary>
        /// Gets or sets the description of the item purchased.
        /// This field is required and limited to 500 characters.
        /// </summary>
        [Required]
        [StringLength(500)]
        [Display(Name = "Description", Description = "Description of the line item or purchase item.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity of items purchased in this line.
        /// </summary>
        [Required]
        [Display(Name = "Quantity", Description = "Quantity of items purchased.")]
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit price of the item.
        /// The price is stored as a numeric(18,2), compatible with PostgreSQL.
        /// </summary>
        [Required]
        [Column(TypeName = "numeric(18,2)")]
        [Display(Name = "Unit Price", Description = "Price per single unit.")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Gets the total amount for this line item (Quantity × UnitPrice).
        /// This is a derived value and not stored in the database.
        /// </summary>
        [NotMapped]
        [Display(Name = "Line Total", Description = "Calculated total for this line (Quantity × UnitPrice).")]
        public decimal LineTotal => Quantity * UnitPrice;

        /// <summary>
        /// Gets or sets the foreign key ID of the associated receipt.
        /// This field is required to maintain the relationship between line items and receipts.
        /// </summary>
        [Required]
        [Display(Name = "Receipt Id", Description = "Foreign key to the parent receipt.")]
        public string ReceiptId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parent receipt that this line item belongs to.
        /// </summary>
        [ForeignKey(nameof(ReceiptId))]
        [Display(Name = "Receipt", Description = "The associated receipt for this line item.")]
        public virtual Receipt Receipt { get; set; } = default!;
    }
}
