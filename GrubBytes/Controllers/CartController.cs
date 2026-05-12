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
        private readonly LogService _logService;
        private readonly EmailService _emailService;
        private readonly PdfService _pdfService;
        


        public CartController(CartService cartService, AppDbContext db,
            UserManager<ApplicationUser> userManager, LogService logService, EmailService emailService,
            PdfService pdfService)
        {
            _cartService = cartService;
            _db = db;
            _userManager = userManager;
            _logService = logService;
            _emailService = emailService;
            _pdfService = pdfService;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            ViewBag.Total = _cartService.GetTotal();
            return View(cart);
        }

        [HttpGet]
        public async Task<IActionResult> Rate(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status != "Completed")
                return RedirectToAction("OrderHistory");

            ViewBag.Order = order;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Rate(int orderId, int menuItemScore,
    int catererScore, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            var firstItem = await _db.MenuItems.FindAsync(order.OrderItems.First().MenuItemId);

            _db.Ratings.Add(new GrubBytes.Models.Rating
            {
                UserId = user!.Id,
                OrderId = orderId,
                MenuItemId = firstItem!.Id,
                CatererId = order.CatererId,
                MenuItemScore = menuItemScore,
                CatererScore = catererScore,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await _logService.LogAsync("RatingSubmitted",
                $"User rated Order #{orderId} — Food: {menuItemScore}/5, Caterer: {catererScore}/5",
                user.Id, orderId);

            return RedirectToAction("OrderHistory");
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

            var itemNames = cart.Select(c => $"{c.Title} x{c.Quantity}").ToList();
            try
            {
                await _emailService.SendOrderConfirmationAsync(
                    user!.Email!, user.FullName, order.Id, order.TotalAmount, itemNames);
                await _logService.LogAsync("EmailSent",
                    $"Order confirmation email sent for Order #{order.Id}", user.Id, order.Id);
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("EmailError",
                    $"Failed to send email for Order #{order.Id}: {ex.Message}",
                    user!.Id, order.Id, "Error");
            }

            await _logService.LogAsync("OrderCreated",
            $"Order #{order.Id} placed for ₺{order.TotalAmount}",
            user!.Id, order.Id);

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


        public async Task<IActionResult> DownloadReceipt(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            var pdf = _pdfService.GenerateReceipt(order);
            return File(pdf, "application/pdf", $"GrubBytes_Receipt_Order{orderId}.pdf");
        }

        public async Task<IActionResult> DownloadAgreement(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            var pdf = _pdfService.GenerateAgreement(order);
            return File(pdf, "application/pdf", $"GrubBytes_Agreement_Order{orderId}.pdf");
        }


        [HttpPost]
        public async Task<IActionResult> Reorder(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            _cartService.ClearCart();

            foreach (var item in order.OrderItems)
            {
                if (item.MenuItem?.IsAvailable == true)
                {
                    _cartService.AddItem(new CartItem
                    {
                        MenuItemId = item.MenuItemId,
                        Title = item.MenuItem.Title,
                        UnitPrice = item.MenuItem.Price,
                        Quantity = item.Quantity
                    });
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int menuItemId)
        {
            var user = await _userManager.GetUserAsync(User);
            var existing = await _db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == user!.Id && f.MenuItemId == menuItemId);

            if (existing != null)
            {
                _db.Favorites.Remove(existing);
            }
            else
            {
                _db.Favorites.Add(new Favorite
                {
                    UserId = user!.Id,
                    MenuItemId = menuItemId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}