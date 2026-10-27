using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Quizzes")] // 👈 FORCES the database table name to be "Quizzes"
    public class Quiz
    {
        [Key]
        public int quiz_id { get; set; }

        public int classroom_id { get; set; }

        public int? module_id { get; set; }

        public string title { get; set; } = string.Empty;

        public string difficulty { get; set; } = "Easy";

        public DateTime created_at { get; set; }

        [ForeignKey("classroom_id")]
        public Classroom? Classroom { get; set; }

        [ForeignKey("module_id")]
        public Module? Module { get; set; }

        public ICollection<QuizQuestion>? Questions { get; set; }
    }
}