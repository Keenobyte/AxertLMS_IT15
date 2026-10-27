using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AxertLMS.Models
{
    public sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token_expires_in")]
        public int? RefreshTokenExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    public sealed class GoogleIdentity
    {
        public required string Subject { get; set; }

        public required string Email { get; set; }

        public bool EmailVerified { get; set; }

        public string? Name { get; set; }

        public string? GivenName { get; set; }

        public string? FamilyName { get; set; }

        public string? Picture { get; set; }
    }

    public sealed class CreateGoogleCalendarEventViewModel
    {
        [Required]
        [StringLength(
            200,
            MinimumLength = 1,
            ErrorMessage = "Event title is required.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(5000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Location { get; set; }

        [Required]
        [Display(Name = "Start")]
        public DateTime Start { get; set; }

        [Required]
        [Display(Name = "End")]
        public DateTime End { get; set; }
    }

    public sealed class GoogleCalendarEventCheckModel
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Location { get; set; }

        public DateTimeOffset? Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public string? AllDayDate { get; set; }

        public string? HtmlLink { get; set; }

        public bool IsAllDay =>
            !string.IsNullOrWhiteSpace(AllDayDate);
    }

    public sealed class GoogleCalendarViewModel
    {
        public bool IsConnected { get; set; }

        public string? GoogleEmail { get; set; }

        public List<GoogleCalendarEventViewModel> Events { get; set; }
            = new();

        public CreateGoogleCalendarEventViewModel CreateEvent { get; set; }
            = new();
    }
}
