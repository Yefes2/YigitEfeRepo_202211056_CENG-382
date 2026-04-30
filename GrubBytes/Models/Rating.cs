namespace GrubBytes.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        public int CatererId { get; set; }
        public int MenuItemScore { get; set; }
        public int CatererScore { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; } = null!;
        public Order Order { get; set; } = null!;
        public MenuItem MenuItem { get; set; } = null!;
        public CatererProfile Caterer { get; set; } = null!;
    }
}