using Ecommerce.Areas.Identity.Data;
using Ecommerce.Data;
using Ecommerce.Models;
using Ecommerce.Models.Web.PaymentGateway;
using Ecommerce.ViewComponents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Specialized;
using System.Security.Claims;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<EcommerceUser> _userManager;

    // TODO: move to appsettings.json (recommended)
    private readonly string _storeId = "espor689b59376bc6a";
    private readonly string _storePassword = "espor689b59376bc6a@ssl";
    private readonly bool _isSandbox = true;

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
    [ValidateAntiForgeryToken]
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
    [ValidateAntiForgeryToken]
    public IActionResult SelectAddress(int addressId)
    {
        HttpContext.Session.SetInt32("SelectedAddressId", addressId);
        return RedirectToAction("ThankYou");
    }

    // Remove address
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveAddress(int addressId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var address = _context.Addresses.FirstOrDefault(a => a.Id == addressId && a.UserId == userId);
        if (address != null)
        {
            _context.Addresses.Remove(address);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    // Review/summary page
    public IActionResult ThankYou()
    {
        var addressId = HttpContext.Session.GetInt32("SelectedAddressId");
        if (addressId == null) return RedirectToAction("Index");

        var address = _context.Addresses.FirstOrDefault(a => a.Id == addressId.Value);
        if (address == null) return RedirectToAction("Index");

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

    // Cash On Delivery
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var addressId = HttpContext.Session.GetInt32("SelectedAddressId");
        if (addressId == null) return RedirectToAction("Index");

        var address = _context.Addresses.FirstOrDefault(a => a.Id == addressId.Value);
        if (address == null) return RedirectToAction("Index");

        var cartItems = _context.Carts.Include(c => c.Product).Where(c => c.UserId == user.Id).ToList();
        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty!";
            return RedirectToAction("Index", "Product");
        }

        var order = new Order
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            OrderStatus = "Pending", // for COD you can set "Placed"
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

        HttpContext.Session.Remove("SelectedAddressId");
        return RedirectToAction("OrderList", "Order");
    }

    // ---------- SSLCommerz: Start Online Payment ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayOnline()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var addressId = HttpContext.Session.GetInt32("SelectedAddressId");
        if (addressId == null) return RedirectToAction("Index");

        var address = _context.Addresses.FirstOrDefault(a => a.Id == addressId.Value);
        if (address == null) return RedirectToAction("Index");

        var cartItems = _context.Carts.Include(c => c.Product).Where(c => c.UserId == user.Id).ToList();
        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty!";
            return RedirectToAction("Index", "Product");
        }

        // Create an order in "PendingPayment" state first
        var order = new Order
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            OrderStatus = "PendingPayment",
            AddressId = address.Id,
            TotalAmount = cartItems.Sum(c => c.Qty * c.Product.Price),
            OrderItems = cartItems.Select(c => new OrderItem
            {
                ProductId = c.ProductId,
                Quantity = c.Qty,
                UnitPrice = c.Product.Price
            }).ToList(),
            // Optional: store temp fields like TranId
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var tranId = $"ORD-{order.Id}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        order.PaymentTransactionId = tranId; // make sure this field exists
        await _context.SaveChangesAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var postData = new NameValueCollection
{
    { "total_amount", order.TotalAmount.ToString("0.00") },
    { "tran_id", tranId },
    //{ "success_url", $"{baseUrl}/Checkout/CheckoutConfirmation" },
    { "success_url", $"{baseUrl}/Checkout/PaymentSuccess?tran_id={tranId}" },
    { "fail_url",    $"{baseUrl}/Checkout/CheckoutFail" },
    { "cancel_url",  $"{baseUrl}/Checkout/CheckoutCancel" },
    { "version", "3.00" },

    // Customer info
    { "cus_name",  user.FullName ?? user.UserName ?? "Customer" },
    { "cus_email", user.Email ?? "no-email@example.com" },
    { "cus_add1",  address.AddressLine ?? "" },
    { "cus_add2",  "" },
    { "cus_city",  address.City ?? "" },
    { "cus_state", address.State ?? "" },
    { "cus_phone", string.IsNullOrWhiteSpace(user.PhoneNumber) ? "01700000000" : user.PhoneNumber },

    //{ "cus_postcode", address.PostalCode ?? "" },
    { "cus_country", address.Country ?? "Bangladesh" }, // <== important
    { "cus_phone", user.PhoneNumber ?? "" },
    { "cus_fax", "" },

    // Shipping info
    { "ship_name",  user.FullName ?? "Customer" },
    { "ship_add1",  address.AddressLine ?? "" },
    { "cus_phone", string.IsNullOrWhiteSpace(user.PhoneNumber) ? "01700000000" : user.PhoneNumber },
    { "ship_add2",  "" },
    { "ship_city",  address.City ?? "" },
    { "ship_state", address.State ?? "" },
    //{ "ship_postcode", address.PostalCode ?? "" },
    { "ship_country", address.Country ?? "Bangladesh" }, // <== important
    { "ship_phone", user.PhoneNumber ?? "" },

    // Order info
    { "shipping_method", "NO" },
    { "num_of_item", cartItems.Sum(c => c.Qty).ToString() },
    { "product_name", $"Order #{order.Id}" },
    { "product_profile", "general" },
    { "product_category", "Ecommerce" },

    // Optional
    { "value_a", order.Id.ToString() }
};




        var sslcz = new SSLCommerzGatewayProcessor(_storeId, _storePassword, _isSandbox);
        var redirectUrl = sslcz.InitiateTransaction(postData);

        // Do not touch cart yet; clear it only after successful payment
        return Redirect(redirectUrl);
    }

    // ---------- SSLCommerz: Success Callback ----------
    //[HttpPost]
    //[AllowAnonymous]

    //public IActionResult CheckoutConfirmation()
    //{
    //    if (!string.IsNullOrEmpty(Request.Form["status"]) && Request.Form["status"] == "VALID")
    //    {
    //        ViewBag.SuccessInfo = "Payment successful!";
    //    }
    //    else
    //    {
    //        ViewBag.SuccessInfo = "There was some error while processing your payment. Please try again.";
    //    }

    //    return View();
    //}
    // Called by SSLCommerz server-to-server for confirmation
    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken] // avoid CSRF issues from external POST
    public IActionResult CheckoutConfirmation()
    {
        var status = Request.Form["status"];
        var tranId = Request.Form["tran_id"];

        if (string.Equals(status, "VALID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(tranId))
        {
            var order = _context.Orders.FirstOrDefault(o => o.PaymentTransactionId == tranId);
            if (order != null && order.OrderStatus != "Paid")
            {
                order.OrderStatus = "Paid";
                _context.SaveChanges();
            }
        }

        return RedirectToAction("PaymentSuccess", new { tran_id = tranId });
    }
    [HttpGet, HttpPost]
    [AllowAnonymous] // allow gateway to hit it without login
    [IgnoreAntiforgeryToken] // avoid token errors on POST
    public IActionResult PaymentSuccess(string tran_id)
    {
        if (string.IsNullOrEmpty(tran_id))
            tran_id = Request.Form["tran_id"];

        var order = _context.Orders
            .FirstOrDefault(o => o.PaymentTransactionId == tran_id);

        if (order != null && order.OrderStatus != "Paid")
        {
            order.OrderStatus = "Paid";

            // Clear cart
            var cartItems = _context.Carts.Where(c => c.UserId == order.UserId).ToList();
            _context.Carts.RemoveRange(cartItems);

            _context.SaveChanges();
        }

        // Optional: If the customer is logged in, you can redirect to their order list
        // Otherwise, show a public "Payment successful" page
        return RedirectToAction("OrderList", "Order");
    }




    // ---------- SSLCommerz: Fail/Cancel Callbacks ----------
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CheckoutFail()
    {
        var tranId = Request.Form["tran_id"].ToString();
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.PaymentTransactionId == tranId);
        if (order != null)
        {
            order.OrderStatus = "PaymentFailed";
            await _context.SaveChangesAsync();
        }
        ViewBag.FailInfo = "There was an error while processing your payment. Please try again.";
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CheckoutCancel()
    {
        var tranId = Request.Form["tran_id"].ToString();
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.PaymentTransactionId == tranId);
        if (order != null)
        {
            order.OrderStatus = "PaymentCancelled";
            await _context.SaveChangesAsync();
        }
        ViewBag.CancelInfo = "Your payment has been cancelled.";
        return View();
    }
}
