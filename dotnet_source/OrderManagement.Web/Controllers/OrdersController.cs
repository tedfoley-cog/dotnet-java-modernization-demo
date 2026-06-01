using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using OrderManagement.Web.Data;
using OrderManagement.Web.Models;
using OrderManagement.Web.Services;

namespace OrderManagement.Web.Controllers
{
    /// <summary>
    /// ASP.NET MVC controller for the order workflow. Uses async action methods
    /// (Task&lt;ActionResult&gt;) over EF6 async queries. Auth is cookie-based via
    /// the [Authorize] filter configured in Web.config.
    /// </summary>
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly OrderContext _db;
        private readonly OrderService _orders;

        public OrdersController()
        {
            _db = new OrderContext();
            _orders = new OrderService(_db, new PricingService());
        }

        // GET: /Orders?customerId=5
        [HttpGet]
        public async Task<ActionResult> Index(int? customerId)
        {
            var query = _db.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status != OrderStatus.Cancelled);

            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Take(100)
                .ToListAsync();

            return View(orders);
        }

        // GET: /Orders/Details/5
        [HttpGet]
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var order = await _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Lines.Select(l => l.Product))
                .FirstOrDefaultAsync(o => o.OrderId == id.Value);

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // POST: /Orders/Submit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Submit(int id)
        {
            var result = await _orders.SubmitOrderAsync(id);
            if (!result.IsApproved)
            {
                TempData["Error"] = result.Reason;
                return RedirectToAction("Details", new { id });
            }

            TempData["Message"] = "Order submitted and credit-approved.";
            return RedirectToAction("Details", new { id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
