using System.ComponentModel.DataAnnotations;

namespace GrubBytes.ViewModels
{
    public class MenuItemViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0.01, 10000)]
        public decimal Price { get; set; }
    }
}