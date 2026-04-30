namespace GrubBytes.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        public int CatererId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public float AverageRating { get; set; }
        public bool IsAvailable { get; set; } = true;

        public CatererProfile Caterer { get; set; } = null!;
        public ICollection<CustomizationOption> CustomizationOptions { get; set; } = new List<CustomizationOption>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}