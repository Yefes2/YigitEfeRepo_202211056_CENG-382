using GrubBytes.Data;
using GrubBytes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrubBytes.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Caterer)
                .Where(o => o.UserId == user!.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var favorites = await _db.Favorites
                .Include(f => f.MenuItem)
                    .ThenInclude(m => m.Caterer)
                .Where(f => f.UserId == user!.Id)
                .ToListAsync();

            var totalSpent = orders.Sum(o => o.TotalAmount);
            var completedOrders = orders.Count(o => o.Status == "Completed");
            var pendingOrders = orders.Count(o => o.Status == "Pending");

            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalSpent = totalSpent;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.Favorites = favorites;
            ViewBag.RecentOrders = orders.Take(5).ToList();

            return View();
        }
    }
}