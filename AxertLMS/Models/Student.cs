using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Students")]
    public class Student
    {
        [Key]
        public int student_id { get; set; }

        [Required]
        public int user_id { get; set; }

        [Required]
        public int section_id { get; set; }

        [Required]
        [MaxLength(20)]
        public required string student_number { get; set; }

        // Navigation Properties (Made optional '?' for safe querying)
        [ForeignKey("user_id")]
        public User? User { get; set; }

        [ForeignKey("section_id")]
        public Section? Section { get; set; }
    }
}