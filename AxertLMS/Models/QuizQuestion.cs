using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("QuizQuestions")] // 👈 FORCES the database table name to be "QuizQuestions"
    public class QuizQuestion
    {
        [Key]
        public int question_id { get; set; }

        public int quiz_id { get; set; }

        public string question_type { get; set; } = string.Empty;

        public string question_text { get; set; } = string.Empty;

        public string? option_a { get; set; }

        public string? option_b { get; set; }

        public string? option_c { get; set; }

        public string? option_d { get; set; }

        public string correct_answer { get; set; } = string.Empty;

        [ForeignKey("quiz_id")]
        public Quiz? Quiz { get; set; }
    }
}