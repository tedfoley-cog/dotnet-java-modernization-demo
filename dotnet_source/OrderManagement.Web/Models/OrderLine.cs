using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagement.Web.Models
{
    /// <summary>
    /// A single priced line on an order. Unit price is snapshotted from the product
    /// at the time the line is added so later price changes do not rewrite history.
    /// </summary>
    [Table("OrderLines")]
    public class OrderLine
    {
        [Key]
        public int OrderLineId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "money")]
        public decimal UnitPrice { get; set; }

        /// <summary>Discount fraction applied to this line (0.00 - 1.00).</summary>
        [Column(TypeName = "decimal")]
        public decimal DiscountRate { get; set; }

        [Column(TypeName = "money")]
        public decimal LineTotal { get; set; }
    }
}
