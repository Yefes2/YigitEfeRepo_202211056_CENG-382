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

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.CatererProfiles
                .Include(c => c.MenuItems)
                .Include(c => c.Orders)
                    .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            if (profile == null) return View();

            var orders = profile.Orders.ToList();
            var menuItems = profile.MenuItems.ToList();

            var ratings = await _db.Ratings
                .Where(r => r.CatererId == profile.Id)
                .ToListAsync();

            // Revenue per day for last 7 days
            var last7 = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .Reverse()
                .ToList();

            var revenueByDay = last7.Select(day => new
            {
                Label = day.ToString("dd MMM"),
                Revenue = orders
                    .Where(o => o.CreatedAt.Date == day)
                    .Sum(o => o.TotalAmount)
            }).ToList();

            // Orders per menu item
            var itemOrderCounts = menuItems.Select(m => new
            {
                Title = m.Title,
                Count = orders
                    .SelectMany(o => o.OrderItems)
                    .Count(oi => oi.MenuItemId == m.Id)
            }).OrderByDescending(x => x.Count).ToList();

            ViewBag.MenuItemCount = menuItems.Count;
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalRevenue = orders.Sum(o => o.TotalAmount);
            ViewBag.AvgRating = ratings.Any()
                ? ratings.Average(r => r.CatererScore).ToString("0.0")
                : "—";
            ViewBag.RevenueDays = revenueByDay.Select(x => x.Label).ToList();
            ViewBag.RevenueValues = revenueByDay.Select(x => x.Revenue).ToList();
            ViewBag.ItemLabels = itemOrderCounts.Select(x => x.Title).ToList();
            ViewBag.ItemCounts = itemOrderCounts.Select(x => x.Count).ToList();

            return View();
        }

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

        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int menuItemId)
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.CatererProfiles
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            var item = await _db.MenuItems
                .FirstOrDefaultAsync(m => m.Id == menuItemId && m.CatererId == profile!.Id);

            if (item == null) return NotFound();

            item.IsAvailable = !item.IsAvailable;
            await _db.SaveChangesAsync();

            return RedirectToAction("Menu");
        }

        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.CatererProfiles
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            if (profile == null) return View(new List<Order>());

            var orders = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.CatererId == profile.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _db.CatererProfiles
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CatererId == profile!.Id);

            if (order == null) return NotFound();

            order.Status = status;
            await _db.SaveChangesAsync();

            return RedirectToAction("Orders");
        }
    }
}