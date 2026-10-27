using System.ComponentModel.DataAnnotations;

namespace AxertLMS.Models
{
    public class ParaphraserViewModel
    {
        [Required(ErrorMessage = "Please enter text to paraphrase.")]
        [StringLength(
            10000,
            MinimumLength = 10,
            ErrorMessage = "Text must contain between 10 and 10,000 characters.")]
        public string Text { get; set; } = string.Empty;

        [Required]
        public string Style { get; set; } = "Standard";

        public string Result { get; set; } = string.Empty;
    }
}