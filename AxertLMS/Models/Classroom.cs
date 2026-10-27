using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Classrooms")]
    public class Classroom
    {
        [Key]
        public int classroom_id { get; set; }

        [Required]
        public int subject_id { get; set; }

        [Required]
        public int teacher_id { get; set; }

        [Required]
        public int section_id { get; set; }

        // Navigation Properties 
        [ForeignKey("subject_id")]
        public Subject? Subject { get; set; }

        [ForeignKey("teacher_id")]
        public Teacher? Teacher { get; set; }

        [ForeignKey("section_id")]
        public Section? Section { get; set; }

        // One-to-Many Collections (Fixes the UI loop errors)
        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}