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

    // Show saved addresses to choose from
    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var addresses = _context.Addresses
                                .Where(a => a.UserId == userId)
                                .ToList();

        return View(addresses); // View with list of user addresses
    }

    // Save a new address
    [HttpPost]
    public IActionResult SaveAddress(Address address)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        address.UserId = userId;

        _context.Add(address);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    // Select existing address and store in session
    [HttpPost]
    public IActionResult SelectAddress(int addressId)
    {
        HttpContext.Session.SetInt32("SelectedAddressId", addressId);
        return RedirectToAction("OrderSummary");
    }

    // Place order using selected address (or new address if submitted)
    [HttpPost]
    public async Task<IActionResult> Index(Address address)
    {
        if (!ModelState.IsValid)
            return View(address);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge(); // force login

        address.UserId = user.Id;
        _context.Add(address);
        await _context.SaveChangesAsync();

        // Save selected address ID in session for later
        HttpContext.Session.SetInt32("SelectedAddressId", address.Id);

        var cartItems = await _context.Carts
                                      .Include(c => c.Product)
                                      .Where(c => c.UserId == user.Id)
                                      .ToListAsync();

        if (!cartItems.Any())
        {
            ModelState.AddModelError("", "Your cart is empty.");
            return View(address);
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

        return RedirectToAction("OrderSummary", new { id = order.Id });
    }

    // Show the order summary using selected address
    
    [HttpGet]
    [HttpGet]
    public IActionResult OrderSummary(int? id)
    {
        var addressId = HttpContext.Session.GetInt32("SelectedAddressId");

        if (addressId == null)
        {
            // Handle missing address id gracefully
            // e.g., redirect back to checkout or show an error
            return RedirectToAction("Index", "Checkout");
        }

        var address = _context.Addresses.FirstOrDefault(a => a.Id == addressId.Value);

        if (address == null)
        {
            // Address not found in DB, handle error
            return RedirectToAction("Index", "Checkout");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var cartItems = _context.Carts
                                .Include(c => c.Product)
                                .Where(c => c.UserId == userId)
                                .ToList();

        var orderItems = cartItems.Select(c => new OrderItem
        {
            ProductId = c.ProductId,
            Product = c.Product,
            Quantity = c.Qty,
            UnitPrice = c.Product.Price
        }).ToList();

        var dummyOrder = new Order
        {
            Id = 0, // or some default
            CreatedAt = DateTime.Now,
            OrderStatus = "Not placed yet",
            TotalAmount = orderItems.Sum(i => i.Quantity * i.UnitPrice)
        };

        var viewModel = new OrderSummaryViewModel
        {
            ShippingAddress = address,
            OrderItems = orderItems,
            Total = dummyOrder.TotalAmount,
            Order = dummyOrder,
            Address = address // make sure this is set
        };

        return View(viewModel);
    }
}
