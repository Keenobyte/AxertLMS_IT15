using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AxertLMS.Data;
using AxertLMS.Helpers;
using AxertLMS.Models;
using AxertLMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AxertLMS.Controllers
{
    [RoleGuard(
        "Student",
        "Teacher",
        "Registrar",
        "Admin",
        "Super Admin")]
    public class GoogleCalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleCalendarService _calendar;
        private readonly GoogleTokenStore _tokens;
        private readonly ILogger<GoogleCalendarController> _logger;

        public GoogleCalendarController(
            ApplicationDbContext context,
            GoogleCalendarService calendar,
            GoogleTokenStore tokens,
            ILogger<GoogleCalendarController> logger)
        {
            _context = context;
            _calendar = calendar;
            _tokens = tokens;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                var model =
                    await BuildPageModelAsync(
                        userId.Value,
                        cancellationToken);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_CALENDAR_PAGE_FAILURE for UserId={UserId}",
                    userId.Value);

                TempData["Error"] =
                    "Google Calendar could not be loaded. " +
                    "Sign out and use Sign in with Google again to refresh Calendar access.";

                return View(
                    new GoogleCalendarPageViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(
            CreateGoogleEventViewModel model,
            CancellationToken cancellationToken)
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            model.Title =
                model.Title?.Trim() ?? string.Empty;

            model.Description =
                model.Description?.Trim();

            model.Location =
                model.Location?.Trim();

            if (model.End <= model.Start)
            {
                ModelState.AddModelError(
                    nameof(model.End),
                    "End time must be later than start time.");
            }

            if (!ModelState.IsValid)
            {
                return await ReturnCalendarWithValidationAsync(
                    model,
                    cancellationToken);
            }

            try
            {
                if (!await _calendar.HasCalendarAccessAsync(
                        userId.Value,
                        cancellationToken))
                {
                    TempData["Error"] =
                        "Google Calendar permission is not available for this account. " +
                        "Sign out and use Sign in with Google again to grant Calendar access.";

                    return RedirectToAction(nameof(Index));
                }

                var createdEvent =
                    await _calendar.CreateEventAsync(
                        userId.Value,
                        model,
                        cancellationToken);

                if (createdEvent == null)
                {
                    TempData["Error"] =
                        "Your Google Calendar authorization is unavailable. " +
                        "Sign out and use Sign in with Google again to refresh Calendar access.";

                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] =
                    "The event was created successfully in your Google Calendar.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_CALENDAR_CREATE_FAILURE for UserId={UserId}",
                    userId.Value);

                TempData["Error"] =
                    "Google Calendar could not create the event. " +
                    "Sign out and use Sign in with Google again to refresh Calendar access.";

                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssignmentEvent(
            int id,
            CancellationToken cancellationToken)
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (!string.Equals(
                    HttpContext.Session.GetString("UserRole"),
                    "Teacher",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            try
            {
                var teacher =
                    await _context.Teachers
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            t => t.user_id == userId.Value,
                            cancellationToken);

                if (teacher == null)
                {
                    TempData["Error"] =
                        "Your teacher account could not be found.";

                    return RedirectToAction(nameof(Index));
                }

                var assignment =
                    await _context.Assignments
                        .AsNoTracking()
                        .Include(a => a.Classroom)
                            .ThenInclude(c => c!.Subject)
                        .Include(a => a.Classroom)
                            .ThenInclude(c => c!.Section)
                        .SingleOrDefaultAsync(
                            a =>
                                a.assignment_id == id &&
                                a.Classroom != null &&
                                a.Classroom.teacher_id == teacher.teacher_id,
                            cancellationToken);

                if (assignment == null ||
                    assignment.Classroom == null)
                {
                    return NotFound();
                }

                if (!await _calendar.HasCalendarAccessAsync(
                        userId.Value,
                        cancellationToken))
                {
                    TempData["Error"] =
                        "Google Calendar permission is not available for this account. " +
                        "Sign out and use Sign in with Google again to grant Calendar access.";

                    return RedirectToAction(
                        "AssignmentDetails",
                        "Teacher",
                        new { id });
                }

                DateTime start = assignment.due_date;
                DateTime end = assignment.due_date.AddMinutes(30);

                string subjectName =
                    assignment.Classroom.Subject?.subject_name
                    ?? "LMS Subject";

                string sectionName =
                    assignment.Classroom.Section?.section_name
                    ?? "LMS Section";

                string description =
                    string.IsNullOrWhiteSpace(assignment.description)
                        ? $"AxertLMS assignment deadline for {subjectName} ({sectionName})."
                        : assignment.description.Trim();

                var createdEvent =
                    await _calendar.CreateEventAsync(
                        userId.Value,
                        $"Assignment Due: {assignment.title}",
                        description,
                        $"{subjectName} - {sectionName}",
                        start,
                        end,
                        cancellationToken);

                if (createdEvent == null)
                {
                    TempData["Error"] =
                        "Your Google Calendar authorization is unavailable. " +
                        "Sign out and use Sign in with Google again to refresh Calendar access.";

                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] =
                    $"The assignment \"{assignment.title}\" was added to your Google Calendar.";

                return RedirectToAction(
                    "AssignmentDetails",
                    "Teacher",
                    new { id = assignment.assignment_id });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_CALENDAR_ASSIGNMENT_FAILURE for UserId={UserId}, AssignmentId={AssignmentId}",
                    userId.Value,
                    id);

                TempData["Error"] =
                    "The assignment could not be added to Google Calendar.";

                return RedirectToAction(
                    "AssignmentDetails",
                    "Teacher",
                    new { id });
            }
        }

        private async Task<GoogleCalendarPageViewModel>
            BuildPageModelAsync(
                int userId,
                CancellationToken cancellationToken)
        {
            var account =
                await _tokens.GetByUserIdAsync(
                    userId,
                    cancellationToken);

            bool hasCalendarAccess =
                await _calendar.HasCalendarAccessAsync(
                    userId,
                    cancellationToken);

            var model =
                new GoogleCalendarPageViewModel
                {
                    IsConnected = hasCalendarAccess,
                    GoogleEmail = account?.GoogleEmail
                };

            if (hasCalendarAccess)
            {
                var events =
                    await _calendar.GetUpcomingEventsAsync(
                        userId,
                        25,
                        cancellationToken);

                model.Events = events.ToList();
            }

            return model;
        }

        private async Task<IActionResult>
            ReturnCalendarWithValidationAsync(
                CreateGoogleEventViewModel input,
                CancellationToken cancellationToken)
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            var model =
                new GoogleCalendarPageViewModel();

            if (userId.HasValue)
            {
                try
                {
                    model =
                        await BuildPageModelAsync(
                            userId.Value,
                            cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "GOOGLE_CALENDAR_VALIDATION_PAGE_FAILURE for UserId={UserId}",
                        userId.Value);
                }
            }

            ViewBag.Form = input;

            return View(
                "Index",
                model);
        }
    }
}
