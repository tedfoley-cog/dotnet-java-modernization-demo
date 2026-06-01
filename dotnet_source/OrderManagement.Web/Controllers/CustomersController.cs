using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using OrderManagement.Web.Data;
using OrderManagement.Web.Models;

namespace OrderManagement.Web.Controllers
{
    /// <summary>
    /// Customer directory with a server-side search/filter, paged with EF6 async LINQ.
    /// </summary>
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly OrderContext _db;

        public CustomersController()
        {
            _db = new OrderContext();
        }

        // GET: /Customers?search=acme&tier=GOLD&page=1
        [HttpGet]
        public async Task<ActionResult> Index(string search, string tier, int page = 1)
        {
            const int pageSize = 20;

            var query = _db.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search) || c.CustomerCode.Contains(search));

            if (!string.IsNullOrWhiteSpace(tier))
                query = query.Where(c => c.PricingTier == tier);

            int total = await query.CountAsync();

            var customers = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (total + pageSize - 1) / pageSize;
            ViewBag.Search = search;
            ViewBag.Tier = tier;
            return View(customers);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
