using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AxertLMS.Data;
using AxertLMS.Models; // Ensure this points to where your ViewModel lives
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;

namespace AxertLMS.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeacherController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            // 1. Authenticate Teacher
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || HttpContext.Session.GetString("UserRole") != "Teacher")
            {
                return RedirectToAction("Login", "Account");
            }

            var teacher = _context.Teachers
                .Include(t => t.User)
                .FirstOrDefault(t => t.user_id == userId);

            if (teacher == null) return RedirectToAction("Login", "Account");

            // 2. Get the Teacher's Classrooms
            var classrooms = _context.Classrooms
                .Include(c => c.Subject)
                .Include(c => c.Section)
                .Where(c => c.teacher_id == teacher.teacher_id)
                .ToList();

            ViewBag.TeacherName = teacher.User!.l_name;

            // 3. REAL DATA: Calculate Total Unique Students
            var totalStudents = _context.Enrollments
                .Include(e => e.Classroom)
                .Where(e => e.Classroom!.teacher_id == teacher.teacher_id)
                .Select(e => e.student_id)
                .Distinct()
                .Count();
            ViewBag.TotalStudents = totalStudents;

            // 4. REAL DATA: Calculate Submissions that need grading
            var totalUngraded = _context.Submissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a!.Classroom)
                .Where(s => s.Assignment!.Classroom!.teacher_id == teacher.teacher_id)
                .Where(s => !_context.Grades.Any(g => g.submission_id == s.submission_id))
                .Count();
            ViewBag.TotalUngraded = totalUngraded;

            // 5. REAL DATA: Calculate Average Grade per Section for the Chart
            var chartLabels = new List<string>();
            var chartData = new List<double>();

            foreach (var classroom in classrooms)
            {
                chartLabels.Add(classroom.Section!.section_name!);

                var averageGrade = _context.Grades
                    .Include(g => g.Submission)
                    .ThenInclude(s => s!.Assignment)
                    .Where(g => g.Submission!.Assignment!.classroom_id == classroom.classroom_id)
                    .Average(g => (double?)g.score) ?? 0;

                chartData.Add(Math.Round(averageGrade, 2));
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartData = chartData;

            return View(classrooms);
        }

        // GET: /Teacher/ClassroomDetails/{id}
        [HttpGet]
        public async Task<IActionResult> ClassroomDetails(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || HttpContext.Session.GetString("UserRole") != "Teacher")
                return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return RedirectToAction("Login", "Account");

            var classroom = await _context.Classrooms
                .Include(c => c.Subject)
                .Include(c => c.Section)
                .Include(c => c.Assignments)
                .Include(c => c.Modules)
                .FirstOrDefaultAsync(c => c.classroom_id == id && c.teacher_id == teacher.teacher_id);

            if (classroom == null) return RedirectToAction("Dashboard");

            var enrolledStudents = await _context.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s!.User)
                .Where(e => e.classroom_id == id)
                .Select(e => e.Student)
                .ToListAsync();

            var announcements = await _context.Announcements
                .Include(a => a.User)
                .Where(a => a.classroom_id == id)
                .OrderByDescending(a => a.created_at)
                .ToListAsync();

            ViewBag.EnrolledStudents = enrolledStudents;
            ViewBag.Announcements = announcements;

            // Pass the Classroom model directly to the dashboard view
            return View(classroom);
        }

        [HttpGet]
        public IActionResult CreateAssignment(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Teacher")
                return RedirectToAction("Login", "Account");

            ViewBag.ClassroomId = id;
            return View();
        }

        [HttpPost]
        public IActionResult CreateAssignment(int classroom_id, string title, DateTime due_date)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = _context.Teachers.FirstOrDefault(t => t.user_id == userId);
            if (teacher == null) return RedirectToAction("Login", "Account");

            var classroom = _context.Classrooms.FirstOrDefault(c => c.classroom_id == classroom_id && c.teacher_id == teacher.teacher_id);
            if (classroom == null) return RedirectToAction("Dashboard");

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("", "Title is required.");
                ViewBag.ClassroomId = classroom_id;
                return View();
            }

            var newAssignment = new Assignment
            {
                classroom_id = classroom_id,
                title = title,
                due_date = due_date
            };

            _context.Assignments.Add(newAssignment);
            _context.SaveChanges();

            return RedirectToAction("ClassroomDetails", new { id = classroom_id });
        }

        [HttpGet]
        public async Task<IActionResult> AssignmentDetails(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || HttpContext.Session.GetString("UserRole") != "Teacher")
                return RedirectToAction("Login", "Account");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.user_id == userId);
            if (teacher == null) return RedirectToAction("Login", "Account");

            // Fetch the assignment details
            var assignment = await _context.Assignments
                .Include(a => a.Classroom)
                .FirstOrDefaultAsync(a => a.assignment_id == id && a.Classroom.teacher_id == teacher.teacher_id);

            if (assignment == null) return RedirectToAction("Dashboard");

            // Build out the grading queue matching your dynamic View logic
            var enrolledStudentIds = await _context.Enrollments
                .Where(e => e.classroom_id == assignment.classroom_id)
                .Select(e => e.student_id)
                .ToListAsync();

            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => enrolledStudentIds.Contains(s.student_id))
                .ToListAsync();

            var submissions = await _context.Submissions
                .Where(s => s.assignment_id == id)
                .ToListAsync();

            var grades = await _context.Grades

                .ToListAsync();

            // Constructing a dynamic matching block for the view layer layout
            var gradingQueue = new List<dynamic>();
            foreach (var student in students)
            {
                var submission = submissions.FirstOrDefault(s => s.student_id == student.student_id);
                Grade? grade = null;

                if (submission != null)
                {
                    grade = grades.FirstOrDefault(
                        g => g.submission_id == submission.submission_id);
                }

                gradingQueue.Add(new
                {
                    Student = student,
                    Submission = submission,
                    Grade = grade
                });
            }

            ViewBag.GradingQueue = gradingQueue;

            // Pass the Assignment model directly to the grading queue view
            return View(assignment);
        }

        [HttpPost]
        public IActionResult SaveGrade(int submission_id, int assignment_id, decimal score, string feedback)
        {
            var existingGrade = _context.Grades.FirstOrDefault(g => g.submission_id == submission_id);

            if (existingGrade != null)
            {
                existingGrade.score = score;
                existingGrade.feedback = feedback;
            }
            else
            {
                _context.Grades.Add(new Grade
                {
                    submission_id = submission_id,
                    score = score,
                    feedback = feedback
                });
            }

            _context.SaveChanges();
            return RedirectToAction("AssignmentDetails", new { id = assignment_id });
        }

        [HttpPost]
        public IActionResult PostAnnouncement(int classroom_id, string message)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var teacher = _context.Teachers.FirstOrDefault(t => t.user_id == userId);
            if (teacher == null) return RedirectToAction("Login", "Account");

            var classroom = _context.Classrooms.FirstOrDefault(c => c.classroom_id == classroom_id && c.teacher_id == teacher.teacher_id);
            if (classroom == null) return RedirectToAction("Dashboard");

            var newAnnouncement = new Announcement
            {
                classroom_id = classroom_id,
                user_id = userId.Value,
                message = message,
                created_at = DateTime.UtcNow.AddHours(8)
            };

            _context.Announcements.Add(newAnnouncement);
            _context.SaveChanges();

            return RedirectToAction("ClassroomDetails", new { id = classroom_id });
        }

        [HttpGet]
        public IActionResult UploadModule(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Teacher")
                return RedirectToAction("Login", "Account");

            ViewBag.ClassroomId = id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadModule(int classroom_id, string title, IFormFile uploadedFile)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = _context.Teachers.FirstOrDefault(t => t.user_id == userId);
            if (teacher == null) return RedirectToAction("Login", "Account");

            var classroom = _context.Classrooms.FirstOrDefault(c => c.classroom_id == classroom_id && c.teacher_id == teacher.teacher_id);
            if (classroom == null) return RedirectToAction("Dashboard");

            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "modules");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(stream);
                }

                var newModule = new Module
                {
                    classroom_id = classroom_id,
                    title = title,
                    file_path = "/uploads/modules/" + uniqueFileName,
                    created_at = DateTime.UtcNow.AddHours(8)
                };

                _context.Modules.Add(newModule);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ClassroomDetails", new { id = classroom_id });
        }

        public IActionResult MyClasses()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || HttpContext.Session.GetString("UserRole") != "Teacher")
                return RedirectToAction("Login", "Account");

            var teacher = _context.Teachers.FirstOrDefault(t => t.user_id == userId);
            if (teacher == null) return RedirectToAction("Login", "Account");

            var classes = _context.Classrooms
                .Include(c => c.Subject)
                .Include(c => c.Section)
                .Where(c => c.teacher_id == teacher.teacher_id)
                .ToList();

            return View(classes);
        }

        public IActionResult GradingCenter()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = _context.Teachers.FirstOrDefault(t => t.user_id == userId);
            if (teacher == null) return RedirectToAction("Login", "Account");

            var classrooms = _context.Classrooms
                .Include(c => c.Subject)
                .Include(c => c.Section)
                .Where(c => c.teacher_id == teacher.teacher_id)
                .ToList();

            var ungradedCounts = new Dictionary<int, int>();
            foreach (var cls in classrooms)
            {
                var count = _context.Submissions
                    .Include(s => s.Assignment)
                    .Where(s => s.Assignment!.classroom_id == cls.classroom_id)
                    .Where(s => !_context.Grades.Any(g => g.submission_id == s.submission_id))
                    .Count();
                ungradedCounts[cls.classroom_id] = count;
            }

            ViewBag.UngradedCounts = ungradedCounts;
            return View(classrooms);
        }

        // ==========================================
        // STEP 2: THE MASTER GRADEBOOK (Refactored)
        // ==========================================
        public IActionResult ClassGradebook(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var teacher = _context.Teachers.FirstOrDefault(t => t.user_id == userId);
            if (teacher == null) return RedirectToAction("Login", "Account");

            var classroom = _context.Classrooms
                .Include(c => c.Subject)
                .Include(c => c.Section)
                .FirstOrDefault(c => c.classroom_id == id && c.teacher_id == teacher.teacher_id);

            if (classroom == null) return RedirectToAction("GradingCenter");

            var students = _context.Enrollments
                .Include(e => e.Student)
                .ThenInclude(s => s!.User)
                .Where(e => e.classroom_id == id)
                .Select(e => e.Student)
                .OrderBy(s => s!.User!.l_name)
                .ToList();

            var assignments = _context.Assignments
                .Where(a => a.classroom_id == id)
                .OrderBy(a => a.Category).ThenBy(a => a.due_date) // Assuming you have Category mapped
                .ToList();

            var assignmentIds = assignments.Select(a => a.assignment_id).ToList();
            var submissions = _context.Submissions.Where(s => assignmentIds.Contains(s.assignment_id)).ToList();
            var submissionIds = submissions.Select(s => s.submission_id).ToList();
            var grades = _context.Grades.Where(g => submissionIds.Contains(g.submission_id)).ToList();

            // 🧠 CLEAN ARCHITECTURE FIX: Populate the ViewModel instead of ViewBag
            var viewModel = new ClassGradebookViewModel
            {
                Classroom = classroom,
                Students = students!,
                Assignments = assignments,
                Submissions = submissions,
                Grades = grades
            };

            return View(viewModel); // Strongly typed rendering
        }

        [HttpGet]
        public IActionResult EvaluateSubmission([FromQuery] int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || HttpContext.Session.GetString("UserRole") != "Teacher")
                return RedirectToAction("Login", "Account");

            var submission = _context.Submissions
                .Include(s => s.Student).ThenInclude(st => st!.User)
                .Include(s => s.Assignment)
                .FirstOrDefault(s => s.submission_id == id);

            if (submission == null) return RedirectToAction("GradingCenter");

            ViewBag.ExistingGrade = _context.Grades.FirstOrDefault(g => g.submission_id == id);

            return View(submission);
        }

        [HttpPost]
        public IActionResult EvaluateSubmission(int submission_id, decimal score, string feedback)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var submission = _context.Submissions.Include(s => s.Assignment).FirstOrDefault(s => s.submission_id == submission_id);
            if (submission == null) return RedirectToAction("GradingCenter");

            var existingGrade = _context.Grades.FirstOrDefault(g => g.submission_id == submission_id);

            if (existingGrade != null)
            {
                existingGrade.score = score;
                existingGrade.feedback = feedback;
            }
            else
            {
                _context.Grades.Add(new Grade
                {
                    submission_id = submission_id,
                    score = score,
                    feedback = feedback
                });
            }

            _context.SaveChanges();

            return RedirectToAction("ClassGradebook", new { id = submission.Assignment!.classroom_id });
        }

        [HttpGet]
        public IActionResult AllAssignment()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null || HttpContext.Session.GetString("UserRole") != "Teacher")
            {
                return RedirectToAction("Login", "Account");
            }

            var teacher = _context.Teachers.FirstOrDefault(t => t.user_id == userId);
            if (teacher == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var assignments = _context.Assignments
                .Include(a => a.Classroom)
                    .ThenInclude(c => c.Subject)
                .Include(a => a.Classroom)
                    .ThenInclude(c => c.Section)
                .Where(a => a.Classroom.teacher_id == teacher.teacher_id)
                .OrderByDescending(a => a.due_date)
                .ToList();

            return View(assignments);
        }

        public IActionResult QuizDetails(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || HttpContext.Session.GetString("UserRole") != "Teacher")
            {
                return RedirectToAction("Login", "Account");
            }

            var teacher = _context.Teachers.FirstOrDefault(t => t.user_id == userId);
            if (teacher == null) return RedirectToAction("Login", "Account");

            var quiz = _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Classroom)
                .FirstOrDefault(q => q.quiz_id == id);

            if (quiz == null)
                return RedirectToAction("Dashboard");

            if (quiz.Classroom!.teacher_id != teacher.teacher_id)
            {
                return RedirectToAction("Dashboard");
            }

            return View(quiz);
        }

    }
}