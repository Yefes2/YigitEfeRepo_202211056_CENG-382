using GrubBytes.Models;
using System.Text.Json;

namespace GrubBytes.Services
{
    public class CartService
    {
        private const string CartKey = "GrubBytes_Cart";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public List<CartItem> GetCart()
        {
            var json = Session.GetString(CartKey);
            return json == null ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json)!;
        }

        public void SaveCart(List<CartItem> cart)
        {
            Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        }

        public void AddItem(CartItem newItem)
        {
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.MenuItemId == newItem.MenuItemId);
            if (existing != null)
                existing.Quantity += newItem.Quantity;
            else
                cart.Add(newItem);
            SaveCart(cart);
        }

        public void RemoveItem(int menuItemId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.MenuItemId == menuItemId);
            SaveCart(cart);
        }

        public void UpdateQuantity(int menuItemId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.MenuItemId == menuItemId);
            if (item != null)
            {
                if (quantity <= 0) cart.Remove(item);
                else item.Quantity = quantity;
            }
            SaveCart(cart);
        }

        public void ClearCart() => Session.Remove(CartKey);

        public decimal GetTotal() => GetCart().Sum(c => c.ItemTotal);
    }
}