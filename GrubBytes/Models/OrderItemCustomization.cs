namespace GrubBytes.Models
{
    public class OrderItemCustomization
    {
        public int Id { get; set; }
        public int OrderItemId { get; set; }
        public int CustomizationOptionId { get; set; }
        public decimal PriceModifier { get; set; }

        public OrderItem OrderItem { get; set; } = null!;
        public CustomizationOption CustomizationOption { get; set; } = null!;
    }
}