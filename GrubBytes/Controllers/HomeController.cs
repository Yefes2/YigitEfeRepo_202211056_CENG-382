using GrubBytes.Data;
using GrubBytes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GrubBytes.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var menuItems = await _db.MenuItems
                .Include(m => m.Caterer)
                .Where(m => m.IsAvailable)
                .ToListAsync();

            // Pass favorite IDs so the view can mark which items are favorited
            if (User.IsInRole("User"))
            {
                var user = await _userManager.GetUserAsync(User);
                var favoriteIds = await _db.Favorites
                    .Where(f => f.UserId == user!.Id)
                    .Select(f => f.MenuItemId)
                    .ToListAsync();
                ViewBag.FavoriteIds = favoriteIds;
            }
            else
            {
                ViewBag.FavoriteIds = new List<int>();
            }

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

        public async Task<IActionResult> CatererProfile(int id)
        {
            var caterer = await _db.CatererProfiles
                .Include(c => c.MenuItems.Where(m => m.IsAvailable))
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (caterer == null) return NotFound();

            var ratings = await _db.Ratings
                .Where(r => r.CatererId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Include(r => r.User)
                .ToListAsync();

            ViewBag.Ratings = ratings;
            return View(caterer);
        }
    }
}