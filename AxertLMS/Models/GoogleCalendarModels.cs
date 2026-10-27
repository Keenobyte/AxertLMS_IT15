using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AxertLMS.Models
{
    public class GoogleCalendarEventViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public DateTimeOffset? Start { get; set; }

        public DateTimeOffset? End { get; set; }

        public string? AllDayStartDate { get; set; }

        public string? HtmlLink { get; set; }

        public bool IsAllDay =>
            !string.IsNullOrWhiteSpace(AllDayStartDate);
    }

    public class GoogleCalendarPageViewModel
    {
        public bool IsConnected { get; set; }

        public string? GoogleEmail { get; set; }

        public List<GoogleCalendarEventViewModel> Events { get; set; }
            = new();
    }

    public class CreateGoogleEventViewModel
    {
        [Required]
        [StringLength(
            200,
            MinimumLength = 1,
            ErrorMessage = "Event title must be between 1 and 200 characters.")]
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
}