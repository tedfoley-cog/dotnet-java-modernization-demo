using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagement.Web.Models
{
    /// <summary>
    /// A sellable item. Mapped Code-First to dbo.Products.
    /// </summary>
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(30)]
        public string Sku { get; set; }

        [Required]
        [StringLength(150)]
        public string Description { get; set; }

        [Column(TypeName = "money")]
        public decimal ListPrice { get; set; }

        /// <summary>Tax class: STD (standard-rated), RED (reduced), EXM (exempt).</summary>
        [Required]
        [StringLength(3)]
        public string TaxClass { get; set; }

        public int QuantityOnHand { get; set; }

        public bool Discontinued { get; set; }
    }
}
