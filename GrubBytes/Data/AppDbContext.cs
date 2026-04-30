using GrubBytes.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GrubBytes.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<CatererProfile> CatererProfiles { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<CustomizationOption> CustomizationOptions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderItemCustomization> OrderItemCustomizations { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<OrderItemCustomization>()
                .HasOne(oc => oc.OrderItem)
                .WithMany(oi => oi.OrderItemCustomizations)
                .HasForeignKey(oc => oc.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItemCustomization>()
                .HasOne(oc => oc.CustomizationOption)
                .WithMany(co => co.OrderItemCustomizations)
                .HasForeignKey(oc => oc.CustomizationOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasPrecision(18, 2);

            builder.Entity<CustomizationOption>()
                .Property(c => c.PriceModifier)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItemCustomization>()
                .Property(oc => oc.PriceModifier)
                .HasPrecision(18, 2);

            builder.Entity<CatererProfile>()
                .HasOne(c => c.User)
                .WithOne(u => u.CatererProfile)
                .HasForeignKey<CatererProfile>(c => c.UserId);

            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.Caterer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CatererId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasOne(r => r.Order)
                .WithMany(o => o.Ratings)
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasOne(r => r.MenuItem)
                .WithMany(m => m.Ratings)
                .HasForeignKey(r => r.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasOne(r => r.Caterer)
                .WithMany()
                .HasForeignKey(r => r.CatererId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Log>()
                .HasOne(l => l.Order)
                .WithMany(o => o.Logs)
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}