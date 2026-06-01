using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using OrderManagement.Web.Data;
using OrderManagement.Web.Models;

namespace OrderManagement.Web.Services
{
    /// <summary>
    /// Order workflow: building a draft, pricing it, and enforcing the credit check
    /// before submission. Uses EF6 async query operators (ToListAsync/FirstOrDefaultAsync)
    /// and LINQ-to-Entities throughout — the canonical async + LINQ patterns the Java
    /// migration must reproduce with Spring Data JPA + Streams + CompletableFuture.
    /// </summary>
    public class OrderService
    {
        private readonly OrderContext _db;
        private readonly PricingService _pricing;

        public OrderService(OrderContext db, PricingService pricing)
        {
            _db = db;
            _pricing = pricing;
        }

        /// <summary>List recent orders for a customer, newest first.</summary>
        public async Task<List<Order>> GetRecentOrdersAsync(int customerId, int take = 25)
        {
            return await _db.Orders
                .Where(o => o.CustomerId == customerId && o.Status != OrderStatus.Cancelled)
                .OrderByDescending(o => o.OrderDate)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Price every line, denormalize totals onto the header. Idempotent — safe to
        /// call again after lines change.
        /// </summary>
        public Order RepriceOrder(Order order, Customer customer)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (customer == null) throw new ArgumentNullException(nameof(customer));

            decimal subTotal = 0m, discountTotal = 0m, taxTotal = 0m;

            foreach (var line in order.Lines)
            {
                decimal gross = line.Product.ListPrice * line.Quantity;
                decimal net = _pricing.PriceLine(line.Product, line.Quantity,
                                                 customer.PricingTier, out var rate);

                line.UnitPrice = line.Product.ListPrice;
                line.DiscountRate = rate;
                line.LineTotal = net;

                subTotal += gross;
                discountTotal += gross - net;
                taxTotal += _pricing.CalculateTax(net, line.Product.TaxClass, customer.TaxRegionCode);
            }

            order.SubTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);
            order.DiscountTotal = Math.Round(discountTotal, 2, MidpointRounding.AwayFromZero);
            order.TaxTotal = Math.Round(taxTotal, 2, MidpointRounding.AwayFromZero);
            order.GrandTotal = order.SubTotal - order.DiscountTotal + order.TaxTotal;
            return order;
        }

        /// <summary>
        /// Submit an order: revalidate pricing, run the credit check against the
        /// customer's outstanding exposure, and advance the status.
        /// </summary>
        public async Task<CreditCheckResult> SubmitOrderAsync(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Lines.Select(l => l.Product))
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new InvalidOperationException($"Order {orderId} not found.");

            var customer = order.Customer;
            RepriceOrder(order, customer);

            if (customer.OnCreditHold)
                return CreditCheckResult.Blocked("Customer is on credit hold.");

            // Outstanding exposure = unpaid invoices + this order's grand total.
            decimal outstanding = await _db.Invoices
                .Where(i => i.Order.CustomerId == customer.CustomerId && !i.Paid)
                .SumAsync(i => (decimal?)i.AmountDue) ?? 0m;

            decimal projected = outstanding + order.GrandTotal;
            if (projected > customer.CreditLimit)
                return CreditCheckResult.Blocked(
                    $"Credit limit exceeded: projected {projected:C} > limit {customer.CreditLimit:C}.");

            order.Status = OrderStatus.Submitted;
            await _db.SaveChangesAsync();
            return CreditCheckResult.Approved(projected, customer.CreditLimit);
        }
    }

    /// <summary>Outcome of the credit check performed at submission time.</summary>
    public class CreditCheckResult
    {
        public bool IsApproved { get; private set; }
        public string Reason { get; private set; }
        public decimal ProjectedExposure { get; private set; }
        public decimal CreditLimit { get; private set; }

        public static CreditCheckResult Approved(decimal exposure, decimal limit) =>
            new CreditCheckResult { IsApproved = true, ProjectedExposure = exposure, CreditLimit = limit };

        public static CreditCheckResult Blocked(string reason) =>
            new CreditCheckResult { IsApproved = false, Reason = reason };
    }
}
