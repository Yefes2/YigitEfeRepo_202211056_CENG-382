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

        // Create caterer profile
        var profile = new GrubBytes.Models.CatererProfile
        {
            UserId = caterer.Id,
            BusinessName = "Street Kings",
            Description = "Bold street food done right."
        };
        scope.ServiceProvider.GetRequiredService<GrubBytes.Data.AppDbContext>()
            .CatererProfiles.Add(profile);
        await scope.ServiceProvider.GetRequiredService<GrubBytes.Data.AppDbContext>()
            .SaveChangesAsync();

        // Seed menu items
        var db = scope.ServiceProvider.GetRequiredService<GrubBytes.Data.AppDbContext>();
        db.MenuItems.AddRange(
            new GrubBytes.Models.MenuItem
            {
                CatererId = profile.Id,
                Title = "Spicy Chicken Wrap",
                Description = "Crispy chicken with sriracha mayo and fresh veggies.",
                Price = 89.90m,
                IsAvailable = true
            },
            new GrubBytes.Models.MenuItem
            {
                CatererId = profile.Id,
                Title = "Smash Burger",
                Description = "Double smashed patty with caramelized onions and pickles.",
                Price = 129.90m,
                IsAvailable = true
            },
            new GrubBytes.Models.MenuItem
            {
                CatererId = profile.Id,
                Title = "Street Fries",
                Description = "Crispy fries with house seasoning and dipping sauce.",
                Price = 49.90m,
                IsAvailable = true
            }
        );
        await db.SaveChangesAsync();
    }
}



app.Run();