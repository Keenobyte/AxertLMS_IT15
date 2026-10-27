using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AxertLMS.Data;
using AxertLMS.Models;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AxertLMS.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- YOUR ORIGINAL HELPER METHOD ---
        private bool IsStudent()
        {
            return HttpContext.Session.GetString("UserRole") == "Student";
        }

        // --- UPGRADED HCI DASHBOARD ---
        public IActionResult Dashboard()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");

            string email = HttpContext.Session.GetString("UserEmail");
            if (email == null) return RedirectToAction("Login", "Account");

            var student = _context.Students
                .Include(s => s.User)
                .Include(s => s.Section)
                .FirstOrDefault(s => s.User != null && s.User.email == email);

            if (student == null) return NotFound("Student profile not found.");

            ViewBag.StudentName = student.User!.f_name;

            // Fetch Enrolled Classrooms
            var enrollments = _context.Enrollments
                .Where(e => e.student_id == student.student_id)
                .Include(e => e.Classroom).ThenInclude(c => c!.Subject)
                .Include(e => e.Classroom).ThenInclude(c => c!.Teacher).ThenInclude(t => t!.User)
                .ToList();

            var classroomIds = enrollments.Select(e => e.classroom_id).ToList();

            // Fetch ALL Assignments for their classes
            var allAssignments = _context.Assignments
                .Include(a => a.Classroom).ThenInclude(c => c!.Subject)
                .Where(a => classroomIds.Contains(a.classroom_id))
                .OrderBy(a => a.due_date)
                .ToList();

            // Fetch Submissions & Grades
            var mySubmissions = _context.Submissions.Where(s => s.student_id == student.student_id).ToList();
            var submissionIds = mySubmissions.Select(s => s.submission_id).ToList();
            var myGrades = _context.Grades.Where(g => submissionIds.Contains(g.submission_id)).ToList();

            // ==========================================
            // HCI DATA AGGREGATION: The Universal To-Do List
            // ==========================================
            var toDoItems = new List<dynamic>();
            int overdueCount = 0;
            int dueThisWeekCount = 0;

            foreach (var assignment in allAssignments)
            {
                var submission = mySubmissions.FirstOrDefault(s => s.assignment_id == assignment.assignment_id);
                bool isSubmitted = submission != null;

                // Store Philippine Time in a variable so we don't have to keep typing it
                DateTime phTime = DateTime.UtcNow.AddHours(8);

                bool isOverdue = !isSubmitted && assignment.due_date < phTime;

                if (isOverdue) overdueCount++;
                if (!isSubmitted && assignment.due_date >= phTime && assignment.due_date <= phTime.AddDays(7)) dueThisWeekCount++;
                toDoItems.Add(new
                {
                    AssignmentId = assignment.assignment_id,
                    Title = assignment.title,
                    Subject = assignment.Classroom!.Subject!.subject_name,
                    DueDate = assignment.due_date,
                    IsSubmitted = isSubmitted,
                    IsOverdue = isOverdue
                });
            }

            ViewBag.ToDoList = toDoItems.Where(t => !t.IsSubmitted).Take(5).ToList(); // Top 5 urgent
            ViewBag.OverdueCount = overdueCount;
            ViewBag.DueThisWeekCount = dueThisWeekCount;

            // ==========================================
            // VISUAL ANALYTICS: Graph Data
            // ==========================================
            var chartLabels = new List<string>();
            var chartData = new List<double>();

            foreach (var enrollment in enrollments)
            {
                chartLabels.Add(enrollment.Classroom!.Subject!.subject_name!);

                var classGrades = myGrades
                    .Where(g => mySubmissions.Any(s => s.submission_id == g.submission_id && s.assignment_id == allAssignments.FirstOrDefault(a => a.classroom_id == enrollment.classroom_id)?.assignment_id))
                    .Select(g => (double)g.score)
                    .ToList();

                double average = classGrades.Any() ? classGrades.Average() : 0;
                chartData.Add(Math.Round(average, 2));
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartData = chartData;
            ViewBag.Enrollments = enrollments;

            // Note: Returning 'enrollments' so it matches the @model in the Dashboard.cshtml view I gave you
            return View(enrollments);
        }

        // --- ALL YOUR ORIGINAL METHODS PRESERVED BELOW ---

        public IActionResult Classrooms()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var student = _context.Students
                .Include(s => s.User)
                .Include(s => s.Section)
                .FirstOrDefault(s => s.User!.email == userEmail);

            var enrolledClasses = _context.Enrollments
                .Where(e => e.student_id == student!.student_id)
                .Include(e => e.Classroom).ThenInclude(c => c.Subject)
                .Include(e => e.Classroom).ThenInclude(c => c.Teacher).ThenInclude(t => t.User)
                .Include(e => e.Classroom).ThenInclude(c => c.Section)
                .Select(e => e.Classroom)
                .ToList();

            return View(enrolledClasses);
        }

        public IActionResult ClassDetails(int id)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var student = _context.Students.Include(s => s.User).FirstOrDefault(s => s.User.email == userEmail);

            bool isEnrolled = _context.Enrollments.Any(e => e.classroom_id == id && e.student_id == student.student_id);
            if (!isEnrolled) return Unauthorized("You are not enrolled in this class.");

            var currentStudent = _context.Students.FirstOrDefault(s => s.User!.email == userEmail);

            var classroom = _context.Classrooms
                .Include(c => c.Subject)
                .Include(c => c.Teacher).ThenInclude(t => t.User)
                .Include(c => c.Section)
                .FirstOrDefault(c => c.classroom_id == id);

            ViewBag.Announcements = _context.Announcements
                .Include(a => a.User)
                .Where(a => a.classroom_id == id)
                .OrderByDescending(a => a.created_at)
                .ToList();

            ViewBag.Modules = _context.Modules.Where(m => m.classroom_id == id).ToList();

            ViewBag.Assignments = _context.Assignments
                .Where(a => a.classroom_id == id)
                .OrderBy(a => a.due_date)
                .ToList();

            ViewBag.Classmates = _context.Students
                .Include(s => s.User)
                .Where(s => s.section_id == classroom!.section_id)
                .OrderBy(s => s.User!.l_name)
                .ToList();

            if (currentStudent != null)
            {
                ViewBag.SubmittedAssignmentIds = _context.Submissions
                    .Where(s => s.student_id == currentStudent.student_id && s.Assignment!.classroom_id == id)
                    .Select(s => s.assignment_id)
                    .ToList();
            }

            return View(classroom);
        }

        [HttpGet]
        public IActionResult SubmitAssignment(int id)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var student = _context.Students.FirstOrDefault(s => s.User!.email == userEmail);
            if (student == null) return NotFound();

            var assignment = _context.Assignments
                .Include(a => a.Classroom)
                .ThenInclude(c => c.Subject)
                .FirstOrDefault(a => a.assignment_id == id);

            if (assignment == null) return NotFound();

            var existingSubmission = _context.Submissions
                .FirstOrDefault(s => s.assignment_id == id && s.student_id == student.student_id);

            ViewBag.HasSubmitted = existingSubmission != null;
            ViewBag.SubmissionDetails = existingSubmission;

            return View(assignment);
        }

        [HttpPost]
        public async Task<IActionResult> UploadSubmission(int assignment_id, int classroom_id, IFormFile studentFile)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var student = _context.Students.FirstOrDefault(s => s.User!.email == userEmail);
            if (student == null) return NotFound();

            if (studentFile == null || studentFile.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction("SubmitAssignment", new { id = assignment_id });
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "submissions");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{student.student_id}_{assignment_id}_{Guid.NewGuid().ToString().Substring(0, 6)}_{Path.GetFileName(studentFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await studentFile.CopyToAsync(stream);
            }

            var newSubmission = new Submission
            {
                assignment_id = assignment_id,
                student_id = student.student_id,
                file_path = $"/uploads/submissions/{uniqueFileName}",
                submitted_at = DateTime.Now
            };

            _context.Submissions.Add(newSubmission);
            await _context.SaveChangesAsync();

            return RedirectToAction("SubmitAssignment", new { id = assignment_id });
        }

        public IActionResult Assignments()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var student = _context.Students.Include(s => s.User).FirstOrDefault(s => s.User!.email == userEmail);
            if (student == null) return NotFound();

            var enrolledClassIds = _context.Enrollments
                .Where(e => e.student_id == student.student_id)
                .Select(e => e.classroom_id)
                .ToList();

            var allAssignments = _context.Assignments
                .Include(a => a.Classroom)
                    .ThenInclude(c => c.Subject)
                .Where(a => enrolledClassIds.Contains(a.classroom_id))
                .OrderBy(a => a.due_date)
                .ToList();

            ViewBag.SubmittedIds = _context.Submissions
                .Where(s => s.student_id == student.student_id)
                .Select(s => s.assignment_id)
                .ToList();

            return View(allAssignments);
        }

        public IActionResult Announcements()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var student = _context.Students.Include(s => s.User).FirstOrDefault(s => s.User!.email == userEmail);
            if (student == null) return NotFound();

            var enrolledClassIds = _context.Enrollments
                .Where(e => e.student_id == student.student_id)
                .Select(e => e.classroom_id)
                .ToList();

            var globalAnnouncements = _context.Announcements
                .Include(a => a.User)
                .Include(a => a.Classroom)
                    .ThenInclude(c => c.Subject)
                .Where(a => enrolledClassIds.Contains(a.classroom_id))
                .OrderByDescending(a => a.created_at)
                .ToList();

            return View(globalAnnouncements);
        }

        public IActionResult Calendar(string targetDate)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var student = _context.Students.Include(s => s.User).FirstOrDefault(s => s.User!.email == userEmail);
            if (student == null) return NotFound();

            DateTime currentDate = DateTime.Today;
            if (!string.IsNullOrEmpty(targetDate))
            {
                DateTime.TryParse(targetDate, out currentDate);
            }

            int diff = (7 + (currentDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = currentDate.AddDays(-1 * diff).Date;
            DateTime endOfWeek = startOfWeek.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            var enrolledClassIds = _context.Enrollments
                .Where(e => e.student_id == student.student_id)
                .Select(e => e.classroom_id)
                .ToList();

            var weeklyAssignments = _context.Assignments
                .Include(a => a.Classroom)
                    .ThenInclude(c => c.Subject)
                .Where(a => enrolledClassIds.Contains(a.classroom_id)
                         && a.due_date >= startOfWeek
                         && a.due_date <= endOfWeek)
                .OrderBy(a => a.due_date)
                .ToList();

            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek;
            ViewBag.PrevWeek = startOfWeek.AddDays(-7).ToString("yyyy-MM-dd");
            ViewBag.NextWeek = startOfWeek.AddDays(7).ToString("yyyy-MM-dd");

            ViewBag.SubmittedIds = _context.Submissions
                .Where(s => s.student_id == student.student_id)
                .Select(s => s.assignment_id)
                .ToList();

            return View(weeklyAssignments);
        }
    }
}