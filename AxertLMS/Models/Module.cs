using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    public class Module
    {
        [Key]
        public int module_id { get; set; }
        public int classroom_id { get; set; }
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string file_path { get; set; } = string.Empty;

        [NotMapped] // 👈 THIS IS THE MAGIC TAG. It tells Entity Framework to ignore this for the database!
        public DateTime created_at { get; set; }

        [ForeignKey("classroom_id")]
        public Classroom? Classroom { get; set; }
    }
}