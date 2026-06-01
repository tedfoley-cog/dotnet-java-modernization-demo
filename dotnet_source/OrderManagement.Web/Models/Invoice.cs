using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagement.Web.Models
{
    /// <summary>
    /// An invoice raised against a shipped order. Mapped Code-First to dbo.Invoices.
    /// </summary>
    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(20)]
        public string InvoiceNumber { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        public DateTime IssuedDate { get; set; }

        public DateTime DueDate { get; set; }

        [Column(TypeName = "money")]
        public decimal AmountDue { get; set; }

        public bool Paid { get; set; }
    }
}
