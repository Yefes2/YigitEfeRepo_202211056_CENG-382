using GrubBytes.Data;
using GrubBytes.Models;
using GrubBytes.Services;
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
        private readonly EmailService _emailService;

        public CatererController(AppDbContext db, UserManager<ApplicationUser> userManager,
            EmailService emailService)
        {
            _db = db;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            var profiles = await _db.CatererProfiles
                .Include(c => c.MenuItems)
                .Include(c => c.Orders)
                    .ThenInclude(o => o.OrderItems)
                .Where(c => c.UserId == user!.Id)
                .ToListAsync();

            if (!profiles.Any()) return View();

            var orders = profiles.SelectMany(p => p.Orders).ToList();
            var menuItems = profiles.SelectMany(p => p.MenuItems).ToList();
            var profileIds = profiles.Select(p => p.Id).ToList();

            var ratings = await _db.Ratings
                .Where(r => profileIds.Contains(r.CatererId))
                .ToListAsync();

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

            var itemOrderCounts = menuItems
                .GroupBy(m => m.Title)
                .Select(g => new
                {
                    Title = g.Key,
                    Count = orders
                        .SelectMany(o => o.OrderItems)
                        .Count(oi => g.Select(m => m.Id).Contains(oi.MenuItemId))
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            ViewBag.MenuItemCount = profiles.Count * 3;
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
            var menuItems = await _db.MenuItems
                .Include(m => m.Caterer)
                .Where(m => m.Caterer.UserId == user!.Id)
                .ToListAsync();

            return View(menuItems);
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

            var item = await _db.MenuItems
                .Include(m => m.Caterer)
                .FirstOrDefaultAsync(m => m.Id == menuItemId && m.Caterer.UserId == user!.Id);

            if (item == null) return NotFound();

            item.IsAvailable = !item.IsAvailable;
            await _db.SaveChangesAsync();

            return RedirectToAction("Menu");
        }

        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            var profileIds = await _db.CatererProfiles
                .Where(c => c.UserId == user!.Id)
                .Select(c => c.Id)
                .ToListAsync();

            var orders = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.Caterer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => profileIds.Contains(o.CatererId))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var user = await _userManager.GetUserAsync(User);
            var profileIds = await _db.CatererProfiles
                .Where(c => c.UserId == user!.Id)
                .Select(c => c.Id)
                .ToListAsync();

            var order = await _db.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId && profileIds.Contains(o.CatererId));

            if (order == null) return NotFound();

            order.Status = status;
            await _db.SaveChangesAsync();

            if (status == "Completed")
            {
                try
                {
                    await _emailService.SendOrderCompletedAsync(
                        order.User!.Email!, order.User.FullName,
                        order.Id, order.TotalAmount);
                }
                catch { /* silent fail */ }
            }

            return RedirectToAction("Orders");
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> DenyOrder(int orderId, string denialReason)
        {
            var user = await _userManager.GetUserAsync(User);
            var profileIds = await _db.CatererProfiles
                .Where(c => c.UserId == user!.Id)
                .Select(c => c.Id)
                .ToListAsync();

            var order = await _db.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId && profileIds.Contains(o.CatererId));

            if (order == null) return NotFound();

            order.Status = "Denied";
            order.DenialReason = denialReason;
            await _db.SaveChangesAsync();

            try
            {
                await _emailService.SendOrderDeniedAsync(
                    order.User!.Email!, order.User.FullName,
                    order.Id, order.TotalAmount, denialReason);
            }
            catch { /* silent fail */ }

            return RedirectToAction("Orders");
        }
    }
}