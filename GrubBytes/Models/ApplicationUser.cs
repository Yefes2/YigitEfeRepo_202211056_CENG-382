using Microsoft.AspNetCore.Identity;

namespace GrubBytes.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CatererProfile> CatererProfiles { get; set; } = new List<CatererProfile>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Log> Logs { get; set; } = new List<Log>();

        public string? ProfileImagePath { get; set; }

    }
}