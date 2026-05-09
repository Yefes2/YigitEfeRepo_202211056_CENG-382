using GrubBytes.Models;
using GrubBytes.Services;
using GrubBytes.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GrubBytes.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly LogService _logService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            LogService logService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logService = logService;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("User"))
                    await _roleManager.CreateAsync(new IdentityRole("User"));

                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);

                // ← ADD THIS
                await _logService.LogAsync("Register", $"New user registered: {model.Email}", user.Id);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                // ← AND THIS
                await _logService.LogAsync("Register",
                    $"Failed registration attempt for {model.Email}.", severity: "Warning");

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                
                await _logService.LogAsync("Login", $"User {model.Email} logged in successfully.", user!.Id);

                var roles = await _userManager.GetRolesAsync(user!);
                if (roles.Contains("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                else if (roles.Contains("Caterer"))
                    return RedirectToAction("Dashboard", "Caterer");
                else
                    return RedirectToAction("Index", "Home");
            }

            
            await _logService.LogAsync("Login",
                $"Failed login attempt for {model.Email}.", severity: "Warning");

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}