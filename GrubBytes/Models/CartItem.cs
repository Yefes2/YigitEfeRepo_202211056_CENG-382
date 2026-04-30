namespace GrubBytes.Models
{
    public class CartItem
    {
        public int MenuItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public List<CartCustomization> Customizations { get; set; } = new();
        public decimal ItemTotal => (UnitPrice + Customizations.Sum(c => c.PriceModifier)) * Quantity;
    }

    public class CartCustomization
    {
        public int OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public decimal PriceModifier { get; set; }
    }
}