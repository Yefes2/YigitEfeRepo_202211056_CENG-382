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
    }
}