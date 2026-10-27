using AxertLMS.Data;
using AxertLMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AxertLMS.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // NOTE: Replace "101" with your actual session or authentication system logic to fetch the logged-in user's ID.
            // Example: int loggedInUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            int loggedInUserId = 101;

            var user = _context.Users.FirstOrDefault(u => u.user_id == loggedInUserId);

            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ProfileViewModel
            {
                UserId = user.user_id,
                Role = user.role,
                FirstName = user.f_name,
                LastName = user.l_name,
                Email = user.email,
                Status = user.status
            };

            // Fetch Role-Specific Information
            if (user.role == "Student")
            {
                var studentInfo = _context.Students
                    .Include(s => s.Section)
                    .FirstOrDefault(s => s.user_id == user.user_id);

                if (studentInfo != null)
                {
                    viewModel.StudentNumber = studentInfo.student_number;
                    viewModel.SectionName = studentInfo.Section?.section_name;
                    viewModel.Strand = studentInfo.Section?.Strand?.strand_name;
                    viewModel.YearLevel = studentInfo.Section?.year_lvl;
                    viewModel.EnrolledClassesCount = _context.Enrollments.Count(e => e.student_id == studentInfo.student_id);
                }
            }
            else if (user.role == "Teacher")
            {
                var teacherInfo = _context.Teachers
                    .FirstOrDefault(t => t.user_id == user.user_id);

                if (teacherInfo != null)
                {
                    viewModel.Specialization = teacherInfo.specialization;
                    viewModel.HandledClassesCount = _context.Classrooms.Count(c => c.teacher_id == teacherInfo.teacher_id);
                }
            }

            return View(viewModel);
        }
    }
}