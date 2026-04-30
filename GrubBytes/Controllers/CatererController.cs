using GrubBytes.Data;
using GrubBytes.Models;
using GrubBytes.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrubBytes.Controllers
{
    [Authorize(Roles = "Caterer")]
    public class CatererController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CatererController(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Dashboard() => View();

        public async Task<IActionResult> Menu()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.CatererProfiles
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            return View(profile?.MenuItems ?? new List<MenuItem>());
        }

        [HttpGet]
        public IActionResult CreateMenuItem() => View();

        [HttpPost]
        public async Task<IActionResult> CreateMenuItem(MenuItemViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.CatererProfiles
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            if (profile == null)
            {
                profile = new CatererProfile
                {
                    UserId = user!.Id,
                    BusinessName = user.FullName,
                    Description = string.Empty
                };
                _db.CatererProfiles.Add(profile);
                await _db.SaveChangesAsync();
            }

            var item = new MenuItem
            {
                CatererId = profile.Id,
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                IsAvailable = true
            };

            _db.MenuItems.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Menu");
        }
    }
}