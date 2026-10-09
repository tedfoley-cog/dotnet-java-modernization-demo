using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using OrderManagement.Web.Models;

namespace OrderManagement.Web.Data
{
    /// <summary>
    /// Entity Framework 6 Code-First context. The connection string is supplied by the
    /// environment (ORDERDB_CONNECTION_STRING), never by committed config.
    /// Maps the five aggregate roots of the order domain.
    /// </summary>
    public class OrderContext : DbContext
    {
        public const string ConnectionStringVariable = "ORDERDB_CONNECTION_STRING";

        public OrderContext() : this(ResolveConnectionString())
        {
        }

        public OrderContext(string connectionString) : base(connectionString)
        {
        }

        private static string ResolveConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    ConnectionStringVariable + " is not set; provide the OrderDb connection string via the environment.");
            }
            return connectionString;
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
