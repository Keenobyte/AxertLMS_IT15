using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AxertLMS.Models
{
    [Table("CurriculumMaps")]
    public class CurriculumMap
    {
        /*
         * LEGACY DATABASE COMPATIBILITY
         *
         * The existing database uses:
         *   curriculum_map_id
         *   subject_id
         *   strand
         *   year_lvl
         *
         * The database cannot be altered, so the CLR model maps to those
         * existing columns instead of expecting strand_id or map_id.
         */

        [Key]
        [Column("curriculum_map_id")]
        public int map_id { get; set; }

        [Required]
        public int subject_id { get; set; }

        /*
         * Existing database column.
         *
         * The legacy schema stores the strand code as text, for example:
         * STEM, HUMSS, ABM, TVL.
         */
        [Required]
        [Column("strand")]
        public required string strand { get; set; }

        /*
         * The legacy CurriculumMaps table does not contain an
         * is_core_subject column.
         *
         * Keep this property available to application code without
         * allowing EF Core to generate SQL for it.
         */
        [NotMapped]
        public bool is_core_subject { get; set; }

        [Required]
        [MaxLength(5)]
        public required string year_lvl { get; set; }

        [ForeignKey(nameof(subject_id))]
        public Subject? Subject { get; set; }

        /*
         * Compatibility properties retained for application code.
         *
         * These are intentionally excluded from EF Core mapping because
         * the existing database has no strand_id or Strand foreign key
         * column in CurriculumMaps.
         */
        [NotMapped]
        public int? strand_id { get; set; }

        [NotMapped]
        public Strand? Strand { get; set; }
    }
}