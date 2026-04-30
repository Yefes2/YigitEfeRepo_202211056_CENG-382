namespace GrubBytes.Models
{
    public class CustomizationOption
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string OptionName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "removable", "addition", "group"
        public decimal PriceModifier { get; set; }
        public bool IsRemovable { get; set; }

        public MenuItem MenuItem { get; set; } = null!;
        public ICollection<OrderItemCustomization> OrderItemCustomizations { get; set; } = new List<OrderItemCustomization>();
    }
}