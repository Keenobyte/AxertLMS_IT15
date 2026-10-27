using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Subjects")]
    public class Subject
    {
        [Key]
        public int subject_id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string subject_name { get; set; }

        public string? description { get; set; }
    }
}