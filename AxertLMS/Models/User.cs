using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int user_id { get; set; }

        [Required]
        public required string role { get; set; } // 'Student', 'Teacher', 'Admin', 'Registrar', 'Super Admin'

        [Required]
        [MaxLength(50)]
        public required string f_name { get; set; }

        [Required]
        [MaxLength(50)]
        public required string l_name { get; set; }

        [Required]
        [MaxLength(255)]
        public required string email { get; set; }

        [Required]
        [MaxLength(255)]
        public required string password { get; set; }

        [Required]
        public required string status { get; set; } // 'Active', 'Inactive'
    }
}