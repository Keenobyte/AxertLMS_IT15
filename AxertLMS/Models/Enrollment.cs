using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Enrollments")]
    public class Enrollment
    {
        [Key]
        public int enrollment_id { get; set; }

        [Required]
        public int student_id { get; set; }

        [Required]
        public int classroom_id { get; set; }

        [Required]
        public DateTime enrollment_date { get; set; }

        // Navigation Properties
        [ForeignKey("student_id")]
        public Student? Student { get; set; }

        [ForeignKey("classroom_id")]
        public Classroom? Classroom { get; set; }
    }
}