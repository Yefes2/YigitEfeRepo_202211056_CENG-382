using GrubBytes.Data;
using GrubBytes.Models;
using GrubBytes.Services;
using GrubBytes.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrubBytes.Controllers
{
    [Authorize(Roles = "User")]
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(CartService cartService, AppDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            ViewBag.Total = _cartService.GetTotal();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int menuItemId, int quantity = 1)
        {
            var item = await _db.MenuItems.FindAsync(menuItemId);
            if (item == null) return NotFound();

            _cartService.AddItem(new CartItem
            {
                MenuItemId = item.Id,
                Title = item.Title,
                UnitPrice = item.Price,
                Quantity = quantity
            });

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int menuItemId)
        {
            _cartService.RemoveItem(menuItemId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int menuItemId, int quantity)
        {
            _cartService.UpdateQuantity(menuItemId, quantity);
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var cart = _cartService.GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            ViewBag.Total = _cartService.GetTotal();
            ViewBag.Cart = cart;
            return View(new PaymentViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Total = _cartService.GetTotal();
                ViewBag.Cart = _cartService.GetCart();
                return View("Checkout", model);
            }

            var cart = _cartService.GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            var user = await _userManager.GetUserAsync(User);

            // Get caterer from first item
            var firstItem = await _db.MenuItems
                .Include(m => m.Caterer)
                .FirstOrDefaultAsync(m => m.Id == cart[0].MenuItemId);

            var order = new Order
            {
                UserId = user!.Id,
                CatererId = firstItem!.CatererId,
                TotalAmount = _cartService.GetTotal(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var cartItem in cart)
            {
                var menuItem = await _db.MenuItems.FindAsync(cartItem.MenuItemId);
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    MenuItemId = cartItem.MenuItemId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice
                };
                _db.OrderItems.Add(orderItem);
                await _db.SaveChangesAsync();

                foreach (var customization in cartItem.Customizations)
                {
                    _db.OrderItemCustomizations.Add(new OrderItemCustomization
                    {
                        OrderItemId = orderItem.Id,
                        CustomizationOptionId = customization.OptionId,
                        PriceModifier = customization.PriceModifier
                    });
                }
            }

            await _db.SaveChangesAsync();
            _cartService.ClearCart();

            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
        }

        public async Task<IActionResult> OrderHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.UserId == user!.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return View(order);
        }
    }
}