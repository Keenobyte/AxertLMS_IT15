using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Assignments")]
    public class Assignment
    {
        [Key]
        public int assignment_id { get; set; }

        [Required]
        public int classroom_id { get; set; }

        [Required]
        [MaxLength(150)]
        public required string title { get; set; }

        public string? description { get; set; }

        public string? attachment_link { get; set; }

        [Required]
        public DateTime due_date { get; set; }

        [Required]
        public string Category { get; set; } = "WW"; // Defaulting to "Written Work" solves legacy controller object initializer errors

        [Required]
        public int MaxScore { get; set; } = 100;

        // Navigation Property
        [ForeignKey("classroom_id")]
        public Classroom? Classroom { get; set; }
    }
}