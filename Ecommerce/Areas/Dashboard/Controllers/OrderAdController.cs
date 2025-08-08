using Ecommerce.Areas.Identity.Data;
using Ecommerce.Data;
using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class OrderAdController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<EcommerceUser> _userManager;

        public OrderAdController(ApplicationDbContext context, UserManager<EcommerceUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Dashboard/OrderAd
        public async Task<IActionResult> OrderList()
        {
            var orders = await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: Dashboard/OrderAd/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // POST: Dashboard/OrderAd/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound();

            order.OrderStatus = status;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} status updated to {status}.";

            return RedirectToAction(nameof(OrderList));
        }
    }

}
