using Ecommerce.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.ViewComponents
{
    public class CartItemCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CartItemCountViewComponent(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int cartCount = 0;

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync((System.Security.Claims.ClaimsPrincipal)User);
                cartCount = await _context.Carts
                    .Where(c => c.UserId == user.Id)
                    .SumAsync(c => c.Qty);
            }
            else
            {
                // If the user is not authenticated, you can return 0 or handle it as needed
                cartCount = 0;
            }
            return View(cartCount);
        }
    }
}
