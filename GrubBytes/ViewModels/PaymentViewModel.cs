using System.ComponentModel.DataAnnotations;

namespace GrubBytes.ViewModels
{
    public class PaymentViewModel
    {
        [Required]
        public string CardHolder { get; set; } = string.Empty;

        [Required, RegularExpression(@"^\d{16}$", ErrorMessage = "Enter a valid 16-digit card number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required, RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Format: MM/YY")]
        public string Expiry { get; set; } = string.Empty;

        [Required, RegularExpression(@"^\d{3}$", ErrorMessage = "Enter a valid 3-digit CVV")]
        public string CVV { get; set; } = string.Empty;
    }
}