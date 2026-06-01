using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagement.Web.Models
{
    /// <summary>Lifecycle state of an order header.</summary>
    public enum OrderStatus
    {
        Draft = 0,
        Submitted = 1,
        Approved = 2,
        Shipped = 3,
        Invoiced = 4,
        Cancelled = 5
    }

    /// <summary>
    /// Order header. Totals are denormalized onto the header by
    /// <see cref="Services.OrderService"/> after pricing the lines.
    /// </summary>
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        public OrderStatus Status { get; set; }

        public DateTime OrderDate { get; set; }

        [Column(TypeName = "money")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "money")]
        public decimal DiscountTotal { get; set; }

        [Column(TypeName = "money")]
        public decimal TaxTotal { get; set; }

        [Column(TypeName = "money")]
        public decimal GrandTotal { get; set; }

        [StringLength(50)]
        public string CreatedBy { get; set; }

        public virtual ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    }
}
