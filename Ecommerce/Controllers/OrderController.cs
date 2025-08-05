using Ecommerce.Areas.Identity.Data;
using Ecommerce.Data;
using Ecommerce.Models;
using Ecommerce.ViewComponents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[Authorize]
    [Area("Dashboard")]

public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<EcommerceUser> _userManager;

    public OrderController(ApplicationDbContext context, UserManager<EcommerceUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Order/OrderSummary/{id}
    public IActionResult OrderSummary(int orderId)
    {
        var order = _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                                   .FirstOrDefault(o => o.Id == orderId);
        var address = _context.Addresses.FirstOrDefault(a => a.Id == order.AddressId);

        if (order == null || address == null)
        {
            return NotFound();
        }

        var viewModel = new OrderSummaryViewModel
        {
            Order = order,
            Address = address,
            OrderItems = order.OrderItems.ToList(),
            Total = order.TotalAmount
        };

        return View(viewModel);
    }
    public async Task<IActionResult> OrderList()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }

        var orders = await _context.Orders
            .Include(o => o.Address)
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

}
