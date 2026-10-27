using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    public class Grade
    {
        [Key]
        public int grade_id { get; set; }

        [ForeignKey("Submission")]
        public int submission_id { get; set; }
        public virtual Submission? Submission { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal score { get; set; }

        public string? feedback { get; set; }
    }
}