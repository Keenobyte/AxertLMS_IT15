using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Sections")]
    public class Section
    {
        [Key]
        public int section_id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string section_name { get; set; }

        [Required]
        public int strand_id { get; set; }

        [Required]
        [MaxLength(5)]
        public required string year_lvl { get; set; }

        [ForeignKey(nameof(strand_id))]
        public Strand? Strand { get; set; }
    }
}