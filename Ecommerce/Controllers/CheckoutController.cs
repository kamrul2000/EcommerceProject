using Ecommerce.Areas.Identity.Data;
using Ecommerce.Data;
using Ecommerce.Models;
using Ecommerce.ViewComponents;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<EcommerceUser> _userManager;

    public CheckoutController(ApplicationDbContext context, UserManager<EcommerceUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Step 1: Show list of addresses
    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var addresses = _context.Addresses.Where(a => a.UserId == userId).ToList();
        return View(addresses);
    }

    // Step 2: Save new address
    [HttpPost]
    public IActionResult SaveAddress(Address address)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        address.UserId = userId;
        _context.Add(address);
        _context.SaveChanges();

        HttpContext.Session.SetInt32("SelectedAddressId", address.Id);

        return RedirectToAction("ThankYou");
    }

    // Step 3: Select existing address
    [HttpPost]
    public IActionResult SelectAddress(int addressId)
    {
        HttpContext.Session.SetInt32("SelectedAddressId", addressId);
        return RedirectToAction("ThankYou");
    }

    // Step 4: Thank You / Review page
    public IActionResult ThankYou()
    {
        var addressId = HttpContext.Session.GetInt32("SelectedAddressId");
        if (addressId == null)
            return RedirectToAction("Index");

        var address = _context.Addresses.FirstOrDefault(a => a.Id == addressId.Value);
        if (address == null)
            return RedirectToAction("Index");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cartItems = _context.Carts.Include(c => c.Product)
                                      .Where(c => c.UserId == userId)
                                      .ToList();

        var orderItems = cartItems.Select(c => new OrderItem
        {
            Product = c.Product,
            ProductId = c.ProductId,
            Quantity = c.Qty,
            UnitPrice = c.Product.Price
        }).ToList();

        var dummyOrder = new Order
        {
            Id = 0,
            CreatedAt = DateTime.Now,
            OrderStatus = "Pending",
            TotalAmount = orderItems.Sum(i => i.Quantity * i.UnitPrice)
        };

        var viewModel = new OrderSummaryViewModel
        {
            ShippingAddress = address,
            Address = address,
            OrderItems = orderItems,
            Order = dummyOrder,
            Total = dummyOrder.TotalAmount
        };

        return View("OrderSummary", viewModel);
    }

    // Step 5: Place Order
    [HttpPost]
    public async Task<IActionResult> PlaceOrder()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var addressId = HttpContext.Session.GetInt32("SelectedAddressId");
        if (addressId == null) return RedirectToAction("Index");

        var address = _context.Addresses.FirstOrDefault(a => a.Id == addressId.Value);
        if (address == null) return RedirectToAction("Index");

        var cartItems = _context.Carts
            .Include(c => c.Product)
            .Where(c => c.UserId == user.Id)
            .ToList();

        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty!";
            return RedirectToAction("Index", "Product");
        }

        var order = new Order
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            OrderStatus = "Pending",
            AddressId = address.Id,
            TotalAmount = cartItems.Sum(c => c.Qty * c.Product.Price),
            OrderItems = cartItems.Select(c => new OrderItem
            {
                ProductId = c.ProductId,
                Quantity = c.Qty,
                UnitPrice = c.Product.Price
            }).ToList()
        };

        _context.Orders.Add(order);
        _context.Carts.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        // Clear address session if needed
        HttpContext.Session.Remove("SelectedAddressId");

        return RedirectToAction("OrderList", "Order");
    }
}
