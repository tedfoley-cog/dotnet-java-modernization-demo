using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using OrderManagement.Web.Data;
using OrderManagement.Web.Models;

namespace OrderManagement.Web.Services
{
    /// <summary>
    /// Raises invoices for shipped orders and generates the next document number.
    /// Demonstrates a small unit-of-work pattern over EF6 plus a LINQ aggregate for
    /// the sequence — both have direct Spring Data JPA equivalents.
    /// </summary>
    public class InvoiceService
    {
        private readonly OrderContext _db;

        public InvoiceService(OrderContext db)
        {
            _db = db;
        }

        public async Task<Invoice> GenerateInvoiceAsync(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new InvalidOperationException($"Order {orderId} not found.");
            if (order.Status != OrderStatus.Shipped)
                throw new InvalidOperationException("Only shipped orders can be invoiced.");

            var issued = DateTime.UtcNow.Date;
            var invoice = new Invoice
            {
                InvoiceNumber = await NextInvoiceNumberAsync(),
                OrderId = order.OrderId,
                IssuedDate = issued,
                DueDate = issued.AddDays(order.Customer.PaymentTermsDays),
                AmountDue = order.GrandTotal,
                Paid = false
            };

            _db.Invoices.Add(invoice);
            order.Status = OrderStatus.Invoiced;
            await _db.SaveChangesAsync();
            return invoice;
        }

        private async Task<string> NextInvoiceNumberAsync()
        {
            int year = DateTime.UtcNow.Year;
            string prefix = $"INV-{year}-";

            // LINQ: find the highest existing suffix for the current year.
            int lastSeq = await _db.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .Select(i => i.InvoiceNumber)
                .Take(1)
                .DefaultIfEmpty(prefix + "0000")
                .Select(n => n)
                .FirstAsync()
                .ContinueWith(t => ParseSuffix(t.Result, prefix));

            return $"{prefix}{(lastSeq + 1):D4}";
        }

        private static int ParseSuffix(string invoiceNumber, string prefix)
        {
            var suffix = invoiceNumber.Substring(prefix.Length);
            return int.TryParse(suffix, out var n) ? n : 0;
        }
    }
}
