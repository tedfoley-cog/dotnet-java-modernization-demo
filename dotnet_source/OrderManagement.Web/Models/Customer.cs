using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagement.Web.Models
{
    /// <summary>
    /// A trading account the business sells to. Mapped Code-First to dbo.Customers.
    /// Credit terms drive the credit check enforced in <see cref="Services.OrderService"/>.
    /// </summary>
    [Table("Customers")]
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(20)]
        public string CustomerCode { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; }

        /// <summary>Discount tier: STANDARD, SILVER, GOLD, PLATINUM. Drives volume discounts.</summary>
        [Required]
        [StringLength(10)]
        public string PricingTier { get; set; }

        [Column(TypeName = "money")]
        public decimal CreditLimit { get; set; }

        /// <summary>Net payment terms in days (e.g. 30 = Net-30).</summary>
        public int PaymentTermsDays { get; set; }

        [StringLength(2)]
        public string TaxRegionCode { get; set; }

        public bool OnCreditHold { get; set; }

        public DateTime CreatedUtc { get; set; }

        // Navigation property — EF lazy-loads the customer's orders.
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
