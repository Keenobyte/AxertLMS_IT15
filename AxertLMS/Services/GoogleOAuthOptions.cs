using System.ComponentModel.DataAnnotations;

namespace AxertLMS.Services
{
    public class GoogleOAuthOptions
    {
        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public string ClientSecret { get; set; } = string.Empty;

        [Required]
        public string RedirectUri { get; set; } = string.Empty;

        [Required]
        public string DefaultTimeZone { get; set; }
            = "Asia/Manila";
    }
}