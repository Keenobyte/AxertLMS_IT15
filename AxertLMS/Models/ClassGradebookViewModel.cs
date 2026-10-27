using System.Collections.Generic;

namespace AxertLMS.Models // Make sure this matches your project's model namespace
{
    public class ClassGradebookViewModel
    {
        public Classroom Classroom { get; set; } = null!;
        public List<Student> Students { get; set; } = new();
        public List<Assignment> Assignments { get; set; } = new();
        public List<Submission> Submissions { get; set; } = new();
        public List<Grade> Grades { get; set; } = new();
    }
}