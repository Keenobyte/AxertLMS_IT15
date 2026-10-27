using System.Collections.Generic;

namespace AxertLMS.Models
{
    public class ManageCurriculumViewModel
    {
        public string SelectedStrand { get; set; } = string.Empty;
        public string SelectedYearLvl { get; set; } = string.Empty;

        // Database Lookups (fixes compiler missing properties errors)
        public List<Strand> Strands { get; set; } = new List<Strand>();
        public List<Subject> Subjects { get; set; } = new List<Subject>();

        // Curriculum Bindings
        public List<Subject> MappedSubjects { get; set; } = new List<Subject>();
        public List<Subject> AllSubjects { get; set; } = new List<Subject>();
    }
}