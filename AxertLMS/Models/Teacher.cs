using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Teachers")]
    public class Teacher
    {
        [Key]
        public int teacher_id { get; set; }

        [Required]
        public int user_id { get; set; }

        [Required]
        public required string specialization { get; set; }

        // Navigation Property
        [ForeignKey("user_id")]
        public User? User { get; set; }
    }
}