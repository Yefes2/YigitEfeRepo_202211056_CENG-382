using GrubBytes.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrubBytes.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var menuItems = await _db.MenuItems
                .Include(m => m.Caterer)
                .Where(m => m.IsAvailable)
                .ToListAsync();

            return View(menuItems);
        }

        public async Task<IActionResult> NearMe()
        {
            var caterers = await _db.CatererProfiles
                .Include(c => c.MenuItems)
                .Where(c => c.MenuItems.Any(m => m.IsAvailable))
                .ToListAsync();

            return View(caterers);
        }
    }
}