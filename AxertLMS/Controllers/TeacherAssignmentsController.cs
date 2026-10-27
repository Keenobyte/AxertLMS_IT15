// File: AxertLMS/Controllers/TeacherAssignmentsController.cs

using AxertLMS.Data;
using AxertLMS.Models;
using AxertLMS.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AxertLMS.Controllers
{
    public class TeacherAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeacherAssignmentsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int classroomId)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (classroomId <= 0)
            {
                return BadRequest("A valid classroom ID is required.");
            }

            var classroom = await _context.Classrooms
                .AsNoTracking()
                .Include(c => c.Subject)
                .Include(c => c.Section)
                .FirstOrDefaultAsync(c =>
                    c.classroom_id == classroomId &&
                    c.teacher_id == teacher.teacher_id);

            if (classroom == null)
            {
                return NotFound(
                    "The classroom does not exist or is not assigned to you.");
            }

            var model = new CreateAssignmentViewModel
            {
                ClassroomId = classroom.classroom_id,
                Category = "WW",
                MaxScore = 100
            };

            SetClassroomTitle(classroom);

            return View(
                "~/Views/Teacher/CreateAssignment.cshtml",
                model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateAssignmentViewModel model)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (model.ClassroomId <= 0)
            {
                return BadRequest("A valid classroom ID is required.");
            }

            var classroom = await _context.Classrooms
                .AsNoTracking()
                .Include(c => c.Subject)
                .Include(c => c.Section)
                .FirstOrDefaultAsync(c =>
                    c.classroom_id == model.ClassroomId &&
                    c.teacher_id == teacher.teacher_id);

            if (classroom == null)
            {
                return NotFound(
                    "The classroom does not exist or is not assigned to you.");
            }

            SetClassroomTitle(classroom);

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Teacher/CreateAssignment.cshtml",
                    model);
            }

            /*
             * datetime-local submits a local wall-clock date and time
             * without a timezone offset.
             *
             * Keep that wall-clock value unchanged in the existing
             * DateTime database column. Do not convert it to UTC here.
             */
            DateTime dueDate = DateTime.SpecifyKind(
                model.DueDate!.Value,
                DateTimeKind.Unspecified);

            var assignment = new Assignment
            {
                classroom_id = classroom.classroom_id,
                title = model.Title.Trim(),
                due_date = dueDate,
                description = string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim(),
                attachment_link = string.IsNullOrWhiteSpace(
                    model.AttachmentLink)
                    ? null
                    : model.AttachmentLink.Trim(),
                Category = model.Category,
                MaxScore = model.MaxScore
            };

            _context.Assignments.Add(assignment);

            await _context.SaveChangesAsync();

            TempData["AssignmentSuccess"] =
                $"Assignment '{assignment.title}' was published successfully.";

            return RedirectToAction(
                "ClassroomDetails",
                "Teacher",
                new { id = classroom.classroom_id });
        }

        private async Task<Teacher?> GetCurrentTeacherAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            string? role = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue ||
                !string.Equals(
                    role,
                    "Teacher",
                    StringComparison.Ordinal))
            {
                return null;
            }

            return await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.user_id == userId.Value);
        }

        private void SetClassroomTitle(Classroom classroom)
        {
            string subjectName =
                classroom.Subject?.subject_name ?? "Classroom";

            string sectionName =
                classroom.Section?.section_name ?? "Section";

            ViewBag.ClassroomTitle =
                $"{subjectName} — {sectionName}";
        }
    }
}