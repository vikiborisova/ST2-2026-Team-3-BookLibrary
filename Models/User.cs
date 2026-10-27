using System.ComponentModel.DataAnnotations;

namespace BookLibrary.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Потребителското име е задължително.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Потребителското име трябва да е между 3 и 50 символа.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Паролата е задължителна.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Паролата трябва да е поне 8 символа.")]
        public string Password { get; set; } = string.Empty;
    }
}
