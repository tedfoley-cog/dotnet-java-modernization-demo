using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using OrderManagement.Web.Models;

namespace OrderManagement.Web.Data
{
    /// <summary>
    /// Entity Framework 6 Code-First context. The connection string "OrderDb" is
    /// resolved from Web.config. Maps the five aggregate roots of the order domain.
    /// </summary>
    public class OrderContext : DbContext
    {
        public OrderContext() : base("name=OrderDb")
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderLine> OrderLines { get; set; }
        public DbSet<Invoice> Invoices { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Keep table names singular-free and avoid cascade-delete cycles.
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerCode)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Lines)
                .WithRequired(l => l.Order)
                .HasForeignKey(l => l.OrderId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<OrderLine>()
                .Property(l => l.DiscountRate)
                .HasPrecision(5, 4);

            base.OnModelCreating(modelBuilder);
        }
    }
}
