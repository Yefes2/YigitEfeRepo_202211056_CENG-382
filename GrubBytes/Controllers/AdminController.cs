using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GrubBytes.Data;
using GrubBytes.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GrubBytes.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalOrders = await _db.Orders.CountAsync();
            var totalRevenue = await _db.Orders.SumAsync(o => o.TotalAmount);
            var totalCaterers = await _db.CatererProfiles.CountAsync();
            var totalItems = await _db.MenuItems.CountAsync();

            // Orders per day last 7 days
            var last7 = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .Reverse()
                .ToList();

            var orders = await _db.Orders.ToListAsync();

            var revenueByDay = last7.Select(day => new
            {
                Label = day.ToString("dd MMM"),
                Revenue = orders
                    .Where(o => o.CreatedAt.Date == day)
                    .Sum(o => o.TotalAmount)
            }).ToList();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalCaterers = totalCaterers;
            ViewBag.TotalItems = totalItems;
            ViewBag.RevenueDays = revenueByDay.Select(x => x.Label).ToList();
            ViewBag.RevenueValues = revenueByDay.Select(x => x.Revenue).ToList();

            return View();
        }

        public async Task<IActionResult> Logs()
        {
            var logs = await _db.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Take(200)
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> ExportLogs()
        {
            var logs = await _db.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Time,EventType,User,Message,Severity");

            foreach (var log in logs)
            {
                var time = log.CreatedAt.ToString("dd MMM yyyy HH:mm:ss");
                var user = log.User?.Email ?? "System";
                var message = log.Message.Replace(",", ";").Replace("\"", "'");
                sb.AppendLine($"{time},{log.EventType},{user},{message},{log.Severity}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"GrubBytes_Logs_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> Users()
        {
            var users = await _db.Users.ToListAsync();
            var userRoles = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles;
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserActive(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // LockoutEnd in the past = active, in the future = locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                // Unlock
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                // Lock for 100 years
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.Caterer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.Status = status;
            await _db.SaveChangesAsync();

            return RedirectToAction("Orders");
        }
    }
}