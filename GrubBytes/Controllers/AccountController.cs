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
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            LogService logService,
            EmailService emailService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logService = logService;
            _emailService = emailService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["ReCaptchaSiteKey"] = _configuration["ReCaptcha:SiteKey"];
            return View();
        }

        
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var captchaToken = Request.Form["g-recaptcha-response"].ToString();
            if (!await VerifyReCaptchaAsync(captchaToken))
            {
                ModelState.AddModelError("", "Please complete the CAPTCHA.");
                ViewData["ReCaptchaSiteKey"] = _configuration["ReCaptcha:SiteKey"];
                return View(model);
            }

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
        public IActionResult Login()
        {
            ViewData["ReCaptchaSiteKey"] = _configuration["ReCaptcha:SiteKey"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var captchaToken = Request.Form["g-recaptcha-response"].ToString();
            if (!await VerifyReCaptchaAsync(captchaToken))
            {
                ModelState.AddModelError("", "Please complete the CAPTCHA.");
                ViewData["ReCaptchaSiteKey"] = _configuration["ReCaptcha:SiteKey"];
                return View(model);
            }

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

        // GET
        public IActionResult ForgotPassword() => View();

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account",
                    new { token, email }, Request.Scheme);

                await _emailService.SendPasswordResetAsync(email, user.FullName ?? "there", resetLink!);
                await _logService.LogAsync("PasswordResetRequested", $"Reset link sent to {email}", userId: user.Id);
            }

            // Always show success — don't reveal if email exists
            TempData["Success"] = "If that email is registered, a reset link has been sent.";
            return RedirectToAction("ForgotPassword");
        }

        // GET
        public IActionResult ResetPassword() => View();

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string email,
                                                        string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction("ResetPassword", new { token, email });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction("ResetPassword", new { token, email });
            }

            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (result.Succeeded)
            {
                await _logService.LogAsync("PasswordReset", $"Password reset for {email}", userId: user.Id);
                TempData["Success"] = "Password reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction("ResetPassword", new { token, email });
        }

        private async Task<bool> VerifyReCaptchaAsync(string token)
        {
            var secret = _configuration["ReCaptcha:SecretKey"];
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={token}",
                null);
            var json = await response.Content.ReadAsStringAsync();
            return json.Contains("\"success\": true");
        }
    }
}