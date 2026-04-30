namespace GrubBytes.Models
{
    public class Log
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int? OrderId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info"; // Info, Warning, Error
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
        public Order? Order { get; set; }
    }
}