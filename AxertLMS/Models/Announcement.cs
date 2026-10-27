using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    public class Announcement
    {
        [Key]
        public int announcement_id { get; set; }

        public int classroom_id { get; set; }

        public int user_id { get; set; }

        public string message { get; set; } = string.Empty;

        public DateTime created_at { get; set; }

        // Navigation properties so EF Core can link the data together
        [ForeignKey("classroom_id")]
        public virtual Classroom? Classroom { get; set; }

        [ForeignKey("user_id")]
        public virtual User? User { get; set; }
    }
}