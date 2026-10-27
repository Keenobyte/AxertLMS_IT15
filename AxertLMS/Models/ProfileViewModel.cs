using System;

namespace AxertLMS.Models
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Student-specific fields
        public string? StudentNumber { get; set; }
        public string? SectionName { get; set; }
        public string? Strand { get; set; }
        public string? YearLevel { get; set; }
        public int EnrolledClassesCount { get; set; }

        // Teacher-specific fields
        public string? Specialization { get; set; }
        public int HandledClassesCount { get; set; }
    }
}