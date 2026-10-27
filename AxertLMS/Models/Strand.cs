using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("Strands")]
    public class Strand
    {
        [Key]
        public int strand_id { get; set; }

        [Required]
        [MaxLength(20)]
        public required string strand_code { get; set; }

        [Required]
        public required string description { get; set; }

        /*
         * Compatibility property for existing Razor pages that
         * display strand_name.
         *
         * It is deliberately NOT mapped to the database.
         */
        [NotMapped]
        public string strand_name => strand_code;

        public override string ToString()
        {
            return strand_code;
        }
    }
}