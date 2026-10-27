using System.Collections.Generic;

namespace AxertLMS.Models
{
    public class StudentViewModel
    {
        public Student? StudentInfo { get; set; }
        public List<Classroom> Classrooms { get; set; } = new List<Classroom>();
    }
}