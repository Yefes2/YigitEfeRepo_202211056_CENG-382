namespace GrubBytes.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int CatererId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ReceiptPath { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;
        public CatererProfile Caterer { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Log> Logs { get; set; } = new List<Log>();
    }
}