using Ecommerce.Areas.Identity.Data;
using Ecommerce.Data;
using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Quic;

namespace Ecommerce.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        public readonly ApplicationDbContext _context;
        public readonly UserManager<EcommerceUser> _userManager;
        public CartController(ApplicationDbContext context, UserManager<EcommerceUser> userManager)
        {
            _context = context;
            _userManager = userManager;

        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);

            var cartItems = await _context.Carts
                .Where(c => c.UserId == currentUser.Id)
                .Include(c => c.Product)
                .ToListAsync();

            return View(cartItems);
        }



        public async Task<IActionResult> AddToCart(int productId, int qty = 1)
        {   var currentuser = await _userManager.GetUserAsync(HttpContext.User);
            var product = await _context.Products.Where(x=>x.Id == productId).FirstOrDefaultAsync();
            if (product == null)
            {
                return BadRequest();
            }

            var cart = new Cart { ProductId = productId, Qty = qty, UserId = currentuser.Id };
            _context.Add(cart);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var cartItem = await _context.Carts.FindAsync(id);

            if (cartItem == null)
            {
                return NotFound();
            }

            _context.Carts.Remove(cartItem);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity([FromBody] QtyUpdateModel model)
        {
            var item = await _context.Carts.FindAsync(model.Id);
            if (item == null)
                return Json(new { success = false });

            item.Qty += model.Change;

            if (item.Qty < 1)
                item.Qty = 1;

            await _context.SaveChangesAsync();

            return Json(new { success = true, newQty = item.Qty });
        }

        public class QtyUpdateModel
        {
            public int Id { get; set; }
            public int Change { get; set; } // +1 or -1
        }



    }
}
