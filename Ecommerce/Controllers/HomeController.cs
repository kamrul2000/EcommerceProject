using Ecommerce.Data;
using Ecommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Ecommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string category)
        {
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                if (category == "Popular")
                    products = products.Where(p => p.Category == "Popular");
                else if (category == "New")
                    products = products.Where(p => p.Category == "New");
            }

            return View(await products.ToListAsync());
        }

        public IActionResult Privacy()
        {
            return View(Privacy);
        }
        public IActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Get related products from the same category, excluding current product
            var related = _context.Products
                .Where(p => p.Category == product.Category && p.Id != product.Id)
                .Take(4) // limit to 4 items
                .ToList();

            ViewBag.RelatedProducts = related;

            return View(product);
        }

    }
}
