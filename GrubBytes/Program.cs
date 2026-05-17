using GrubBytes.Data;
using GrubBytes.Models;
using GrubBytes.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<EmailService>();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddScoped<PdfService>();
builder.Services.AddHttpClient();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Caterer", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed default admin
    var adminEmail = "admin@grubbytes.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "GrubBytes Admin",
            Role = "Admin",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        await userManager.CreateAsync(admin, "Admin123!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    // Seed test caterer
    var catererEmail = "caterer@grubbytes.com";
    if (await userManager.FindByEmailAsync(catererEmail) == null)
    {
        var caterer = new ApplicationUser
        {
            UserName = catererEmail,
            Email = catererEmail,
            FullName = "Test Caterer",
            Role = "Caterer",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        await userManager.CreateAsync(caterer, "Caterer123!");
        await userManager.AddToRoleAsync(caterer, "Caterer");


        var db = scope.ServiceProvider.GetRequiredService<GrubBytes.Data.AppDbContext>();

        var districts = new[]
        {
    new { Name = "Altındağ",     Lat = 39.9425, Lng = 32.8947 },
    new { Name = "Akyurt",       Lat = 40.1338, Lng = 33.0858 },
    new { Name = "Ayaş",         Lat = 40.0186, Lng = 32.3364 },
    new { Name = "Bala",         Lat = 39.5597, Lng = 33.1211 },
    new { Name = "Beypazarı",    Lat = 40.1681, Lng = 31.9217 },
    new { Name = "Çamlıdere",    Lat = 40.4933, Lng = 32.4881 },
    new { Name = "Çankaya",      Lat = 39.9032, Lng = 32.8597 },
    new { Name = "Çubuk",        Lat = 40.2333, Lng = 33.0333 },
    new { Name = "Elmadağ",      Lat = 39.9167, Lng = 33.2333 },
    new { Name = "Etimesgut",    Lat = 39.9478, Lng = 32.6761 },
    new { Name = "Evren",        Lat = 39.0228, Lng = 33.5208 },
    new { Name = "Gölbaşı",      Lat = 39.7883, Lng = 32.8042 },
    new { Name = "Güdül",        Lat = 40.2167, Lng = 32.2417 },
    new { Name = "Haymana",      Lat = 39.4333, Lng = 32.4972 },
    new { Name = "Kahramankazan", Lat = 40.2272, Lng = 32.6878 },
    new { Name = "Kalecik",      Lat = 40.0958, Lng = 33.4139 },
    new { Name = "Keçiören",     Lat = 39.9975, Lng = 32.8642 },
    new { Name = "Kızılcahamam", Lat = 40.4681, Lng = 32.6508 },
    new { Name = "Mamak",        Lat = 39.9333, Lng = 32.9333 },
    new { Name = "Nallıhan",     Lat = 40.1861, Lng = 31.3542 },
    new { Name = "Polatlı",      Lat = 39.5833, Lng = 32.1472 },
    new { Name = "Pursaklar",    Lat = 40.0333, Lng = 32.9000 },
    new { Name = "Sincan",       Lat = 39.9736, Lng = 32.5822 },
    new { Name = "Şereflikoçhisar", Lat = 38.9372, Lng = 33.5344 },
    new { Name = "Yenimahalle",  Lat = 39.9667, Lng = 32.7833 },
};

        foreach (var district in districts)
        {
            var profile = new GrubBytes.Models.CatererProfile
            {
                UserId = caterer.Id,
                BusinessName = $"Street Kings {district.Name}",
                Description = "Bold street food done right.",
                Latitude = district.Lat,
                Longitude = district.Lng
            };
            db.CatererProfiles.Add(profile);
            await db.SaveChangesAsync();

            db.MenuItems.AddRange(
                new GrubBytes.Models.MenuItem
                {
                    CatererId = profile.Id,
                    Title = "Spicy Chicken Wrap",
                    Description = "Crispy chicken with sriracha mayo and fresh veggies.",
                    Price = 89.90m,
                    IsAvailable = true,
                    ImagePath = "/uploads/menu/spicy-chicken-wrap.jpg"
                },
                new GrubBytes.Models.MenuItem
                {
                    CatererId = profile.Id,
                    Title = "Smash Burger",
                    Description = "Double smashed patty with caramelized onions and pickles.",
                    Price = 129.90m,
                    IsAvailable = true,
                    ImagePath = "/uploads/menu/smash-burger.jpg"
                },
                new GrubBytes.Models.MenuItem
                {
                    CatererId = profile.Id,
                    Title = "Street Fries",
                    Description = "Crispy fries with house seasoning and dipping sauce.",
                    Price = 49.90m,
                    IsAvailable = true,
                    ImagePath = "/uploads/menu/street-fries.jpg"
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
app.Run();