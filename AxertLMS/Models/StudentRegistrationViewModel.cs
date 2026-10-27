using System.ComponentModel.DataAnnotations;

namespace AxertLMS.Models
{
    public sealed class StudentRegistrationViewModel
    {
        [Required]
        [StringLength(
            50,
            MinimumLength = 2,
            ErrorMessage = "First name must be between 2 and 50 characters.")]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(
            50,
            MinimumLength = 2,
            ErrorMessage = "Last name must be between 2 and 50 characters.")]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Enter a valid Gmail address.")]
        [StringLength(255)]
        [Display(Name = "Gmail address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            72,
            MinimumLength = 8,
            ErrorMessage = "Password must be between 8 and 72 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(
            nameof(Password),
            ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}