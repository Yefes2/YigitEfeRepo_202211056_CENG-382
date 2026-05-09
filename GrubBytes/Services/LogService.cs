using GrubBytes.Data;
using GrubBytes.Models;

namespace GrubBytes.Services
{
    public class LogService
    {
        private readonly AppDbContext _db;

        public LogService(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string eventType, string message,
            string? userId = null, int? orderId = null, string severity = "Info")
        {
            _db.Logs.Add(new Log
            {
                EventType = eventType,
                Message = message,
                UserId = userId,
                OrderId = orderId,
                Severity = severity,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}