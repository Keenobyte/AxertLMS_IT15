// File: AxertLMS/ViewModels/CreateAssignmentViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace AxertLMS.ViewModels
{
    public class CreateAssignmentViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Invalid classroom.")]
        public int ClassroomId { get; set; }

        [Required(ErrorMessage = "Assignment title is required.")]
        [StringLength(
            150,
            MinimumLength = 2,
            ErrorMessage = "The title must be between 2 and 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a due date and time.")]
        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }

        [StringLength(
            5000,
            ErrorMessage = "Instructions cannot exceed 5,000 characters.")]
        public string? Description { get; set; }

        [StringLength(
            2048,
            ErrorMessage = "The reference URL cannot exceed 2,048 characters.")]
        [Url(ErrorMessage = "Enter a valid reference URL.")]
        public string? AttachmentLink { get; set; }

        [Required(ErrorMessage = "Select an assignment category.")]
        [RegularExpression(
            "^(WW|PT|QA)$",
            ErrorMessage = "Select a valid assignment category.")]
        public string Category { get; set; } = "WW";

        [Range(
            1,
            10000,
            ErrorMessage = "The maximum score must be between 1 and 10,000.")]
        public int MaxScore { get; set; } = 100;
    }
}