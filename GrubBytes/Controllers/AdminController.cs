using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GrubBytes.Data;
using Microsoft.EntityFrameworkCore;

namespace GrubBytes.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Dashboard() => View();

        public async Task<IActionResult> Logs()
        {
            var logs = await _db.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Take(200)
                .ToListAsync();

            return View(logs);
        }
    }
}