using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Submissions")]
    public class Submission
    {
        [Key]
        public int submission_id { get; set; }

        [Required]
        public int assignment_id { get; set; }

        [Required]
        public int student_id { get; set; }

        [MaxLength(512)]
        public string? file_path { get; set; }

        public string? submission_text { get; set; }

        [Required]
        public DateTime submitted_at { get; set; }

        // Plagiarism Tracking Infrastructure
        public double PlagiarismPercentage { get; set; }
        public string? PlagiarismReport { get; set; }

        // Navigation properties
        [ForeignKey("assignment_id")]
        public Assignment? Assignment { get; set; }

        [ForeignKey("student_id")]
        public Student? Student { get; set; }
    }
}