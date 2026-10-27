using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AxertLMS.Data;
using AxertLMS.Helpers;
using AxertLMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AxertLMS.Controllers
{
    [RoleGuard("Registrar", "Super Admin")]
    public class RegistrarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrarController> _logger;

        public RegistrarController(
            ApplicationDbContext context,
            ILogger<RegistrarController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // PENDING STUDENT APPLICATIONS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> RegisterStudent(
            CancellationToken cancellationToken)
        {
            var pendingUsers =
                await _context.Users
                    .AsNoTracking()
                    .Where(
                        u =>
                            u.role == "Student" &&
                            u.status == "Pending" &&
                            !_context.Students.Any(
                                s =>
                                    s.user_id ==
                                    u.user_id))
                    .OrderBy(u => u.l_name)
                    .ThenBy(u => u.f_name)
                    .ToListAsync(
                        cancellationToken);

            var sections =
                await _context.Sections
                    .AsNoTracking()
                    .Include(s => s.Strand)
                    .OrderBy(s => s.year_lvl)
                    .ThenBy(
                        s => s.Strand!.strand_code)
                    .ThenBy(
                        s => s.section_name)
                    .ToListAsync(
                        cancellationToken);

            ViewBag.PendingUsers =
                pendingUsers;

            ViewBag.Sections =
                sections;

            return View();
        }

        // ============================================================
        // REGISTRAR DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard(
            CancellationToken cancellationToken)
        {
            ViewBag.TotalStudents =
                await _context.Students.CountAsync(
                    cancellationToken);

            ViewBag.TotalTeachers =
                await _context.Teachers.CountAsync(
                    cancellationToken);

            ViewBag.TotalSections =
                await _context.Sections.CountAsync(
                    cancellationToken);

            ViewBag.TotalSubjects =
                await _context.Subjects.CountAsync(
                    cancellationToken);

            ViewBag.PendingStudents =
                await _context.Users
                    .Where(
                        u =>
                            u.role == "Student" &&
                            u.status == "Pending" &&
                            !_context.Students.Any(
                                s =>
                                    s.user_id ==
                                    u.user_id))
                    .CountAsync(
                        cancellationToken);

            var recentStudents =
                await _context.Students
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => s.Section)
                        .ThenInclude(s => s!.Strand)
                    .OrderByDescending(
                        s => s.student_id)
                    .Take(10)
                    .ToListAsync(
                        cancellationToken);

            return View(
                recentStudents);
        }

        // ============================================================
        // REGISTER TEACHER
        // ============================================================

        [HttpGet]
        public IActionResult RegisterTeacher()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterTeacher(
            string f_name,
            string l_name,
            string email,
            string password,
            string specialization,
            CancellationToken cancellationToken)
        {
            f_name =
                f_name?.Trim() ?? string.Empty;

            l_name =
                l_name?.Trim() ?? string.Empty;

            email =
                email?.Trim().ToLowerInvariant()
                ?? string.Empty;

            if (
                string.IsNullOrWhiteSpace(f_name) ||
                string.IsNullOrWhiteSpace(l_name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage =
                    "All required teacher fields must be completed.";

                return View();
            }

            bool emailExists =
                await _context.Users
                    .AnyAsync(
                        u => u.email == email,
                        cancellationToken);

            if (emailExists)
            {
                ViewBag.ErrorMessage =
                    "An account with this email already exists.";

                return View();
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                var newUser =
                    new User
                    {
                        f_name = f_name,
                        l_name = l_name,
                        email = email,
                        password =
                            PasswordHelper.Hash(password),
                        role = "Teacher",
                        status = "Active"
                    };

                _context.Users.Add(
                    newUser);

                await _context.SaveChangesAsync(
                    cancellationToken);

                var newTeacher =
                    new Teacher
                    {
                        user_id =
                            newUser.user_id,

                        specialization =
                            specialization?.Trim()
                            ?? string.Empty
                    };

                _context.Teachers.Add(
                    newTeacher);

                await _context.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                TempData["SuccessMessage"] =
                    $"Successfully registered Faculty: " +
                    $"{f_name} {l_name}!";

                return RedirectToAction(
                    nameof(RegisterTeacher));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "TEACHER_REGISTRATION_DATABASE_ERROR");

                ViewBag.ErrorMessage =
                    "The teacher account could not be created because of a database constraint.";

                return View();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "TEACHER_REGISTRATION_FAILURE");

                ViewBag.ErrorMessage =
                    "Registration failed.";

                return View();
            }
        }

        // ============================================================
        // MANAGE SECTIONS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ManageSections(
            CancellationToken cancellationToken)
        {
            var sections =
                await _context.Sections
                    .AsNoTracking()
                    .Include(s => s.Strand)
                    .OrderBy(s => s.year_lvl)
                    .ThenBy(
                        s => s.section_name)
                    .ToListAsync(
                        cancellationToken);

            return View(
                sections);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSection(
            string section_name,
            int strand_id,
            string year_lvl,
            CancellationToken cancellationToken)
        {
            section_name =
                section_name?.Trim() ?? string.Empty;

            year_lvl =
                year_lvl?.Trim() ?? string.Empty;

            if (
                string.IsNullOrWhiteSpace(section_name) ||
                string.IsNullOrWhiteSpace(year_lvl))
            {
                TempData["ErrorMessage"] =
                    "Section name and year level are required.";

                return RedirectToAction(
                    nameof(ManageSections));
            }

            bool strandExists =
                await _context.Strands.AnyAsync(
                    s =>
                        s.strand_id ==
                        strand_id,
                    cancellationToken);

            if (!strandExists)
            {
                TempData["ErrorMessage"] =
                    "Invalid strand selected.";

                return RedirectToAction(
                    nameof(ManageSections));
            }

            try
            {
                var newSection =
                    new Section
                    {
                        section_name =
                            section_name,

                        strand_id =
                            strand_id,

                        year_lvl =
                            year_lvl
                    };

                _context.Sections.Add(
                    newSection);

                await _context.SaveChangesAsync(
                    cancellationToken);

                TempData["SuccessMessage"] =
                    "Section added successfully.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "SECTION_CREATE_DATABASE_ERROR");

                TempData["ErrorMessage"] =
                    "The section could not be saved because of a database constraint.";
            }

            return RedirectToAction(
                nameof(ManageSections));
        }

        // ============================================================
        // MANAGE SUBJECTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ManageSubjects(
            CancellationToken cancellationToken)
        {
            var subjects =
                await _context.Subjects
                    .AsNoTracking()
                    .OrderBy(
                        s => s.subject_name)
                    .ToListAsync(
                        cancellationToken);

            return View(
                subjects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubject(
            string subject_name,
            string description,
            CancellationToken cancellationToken)
        {
            subject_name =
                subject_name?.Trim()
                ?? string.Empty;

            description =
                description?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    subject_name))
            {
                TempData["ErrorMessage"] =
                    "Subject name is required.";

                return RedirectToAction(
                    nameof(ManageSubjects));
            }

            var newSubject =
                new Subject
                {
                    subject_name =
                        subject_name,

                    description =
                        description
                };

            _context.Subjects.Add(
                newSubject);

            await _context.SaveChangesAsync(
                cancellationToken);

            TempData["SuccessMessage"] =
                "Subject added successfully.";

            return RedirectToAction(
                nameof(ManageSubjects));
        }

        // ============================================================
        // ASSIGN FACULTY
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AssignFaculty(
            CancellationToken cancellationToken)
        {
            ViewBag.Teachers =
                await _context.Teachers
                    .AsNoTracking()
                    .Include(t => t.User)
                    .ToListAsync(
                        cancellationToken);

            ViewBag.Subjects =
                await _context.Subjects
                    .AsNoTracking()
                    .OrderBy(
                        s => s.subject_name)
                    .ToListAsync(
                        cancellationToken);

            ViewBag.Sections =
                await _context.Sections
                    .AsNoTracking()
                    .Include(s => s.Strand)
                    .OrderBy(
                        s => s.section_name)
                    .ToListAsync(
                        cancellationToken);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignFaculty(
            int teacher_id,
            int subject_id,
            int section_id,
            CancellationToken cancellationToken)
        {
            bool teacherExists =
                await _context.Teachers.AnyAsync(
                    t =>
                        t.teacher_id ==
                        teacher_id,
                    cancellationToken);

            bool subjectExists =
                await _context.Subjects.AnyAsync(
                    s =>
                        s.subject_id ==
                        subject_id,
                    cancellationToken);

            bool sectionExists =
                await _context.Sections.AnyAsync(
                    s =>
                        s.section_id ==
                        section_id,
                    cancellationToken);

            if (
                !teacherExists ||
                !subjectExists ||
                !sectionExists)
            {
                TempData["ErrorMessage"] =
                    "Invalid teacher, subject, or section.";

                return RedirectToAction(
                    nameof(AssignFaculty));
            }

            bool assignmentExists =
                await _context.Classrooms
                    .AnyAsync(
                        c =>
                            c.section_id ==
                                section_id &&
                            c.subject_id ==
                                subject_id,
                        cancellationToken);

            if (assignmentExists)
            {
                TempData["ErrorMessage"] =
                    "This section already has a teacher for this subject.";

                return RedirectToAction(
                    nameof(AssignFaculty));
            }

            try
            {
                var classroom =
                    new Classroom
                    {
                        teacher_id =
                            teacher_id,

                        subject_id =
                            subject_id,

                        section_id =
                            section_id
                    };

                _context.Classrooms.Add(
                    classroom);

                await _context.SaveChangesAsync(
                    cancellationToken);

                TempData["SuccessMessage"] =
                    "Faculty successfully assigned.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "FACULTY_ASSIGNMENT_DATABASE_ERROR");

                TempData["ErrorMessage"] =
                    "The faculty assignment could not be saved.";
            }

            return RedirectToAction(
                nameof(AssignFaculty));
        }

        // ============================================================
        // APPROVE STUDENT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterStudent(
            int user_id,
            int section_id,
            CancellationToken cancellationToken)
        {
            if (user_id <= 0 ||
                section_id <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid student or section.";

                return RedirectToAction(
                    nameof(RegisterStudent));
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                /*
                 * Locking is intentionally kept simple here.
                 * The database unique indexes below also protect
                 * against duplicate Student/Enrollment records.
                 */
                var pendingUser =
                    await _context.Users
                        .SingleOrDefaultAsync(
                            u =>
                                u.user_id ==
                                    user_id &&
                                u.role ==
                                    "Student" &&
                                u.status ==
                                    "Pending",
                            cancellationToken);

                if (pendingUser == null)
                {
                    TempData["ErrorMessage"] =
                        "The selected student application is no longer pending.";

                    await transaction.RollbackAsync(
                        cancellationToken);

                    return RedirectToAction(
                        nameof(RegisterStudent));
                }

                bool studentExists =
                    await _context.Students.AnyAsync(
                        s =>
                            s.user_id ==
                            user_id,
                        cancellationToken);

                if (studentExists)
                {
                    TempData["ErrorMessage"] =
                        "This student has already been processed.";

                    await transaction.RollbackAsync(
                        cancellationToken);

                    return RedirectToAction(
                        nameof(RegisterStudent));
                }

                var section =
                    await _context.Sections
                        .Include(s => s.Strand)
                        .SingleOrDefaultAsync(
                            s =>
                                s.section_id ==
                                section_id,
                            cancellationToken);

                if (section == null)
                {
                    TempData["ErrorMessage"] =
                        "The selected section does not exist.";

                    await transaction.RollbackAsync(
                        cancellationToken);

                    return RedirectToAction(
                        nameof(RegisterStudent));
                }

                /*
                 * Student number is NEVER accepted from the browser.
                 */
                var newStudent =
                    new Student
                    {
                        user_id =
                            user_id,

                        section_id =
                            section_id,

                        student_number =
                            "TEMP"
                    };

                _context.Students.Add(
                    newStudent);

                await _context.SaveChangesAsync(
                    cancellationToken);

                /*
                 * Database identity gives us the permanent Student ID.
                 */
                newStudent.student_number =
                    $"{DateTime.Now.Year}-" +
                    $"{newStudent.student_id:D4}";

                /*
                 * LEGACY DATABASE COMPATIBILITY
                 *
                 * The existing CurriculumMaps table stores the strand
                 * as text in CurriculumMaps.strand. It does not have
                 * a strand_id column.
                 *
                 * Section.strand_id is resolved through the already
                 * loaded Strand navigation and compared to the legacy
                 * curriculum strand code.
                 */
                if (section.Strand == null ||
                    string.IsNullOrWhiteSpace(
                        section.Strand.strand_code))
                {
                    throw new InvalidOperationException(
                        "The selected section is not associated with a valid strand.");
                }

                var sectionStrandCode =
                    section.Strand.strand_code.Trim();

                var requiredSubjectIds =
                    await _context.CurriculumMaps
                        .AsNoTracking()
                        .Where(
                            map =>
                                map.year_lvl ==
                                    section.year_lvl &&
                                map.strand ==
                                    sectionStrandCode)
                        .Select(
                            map =>
                                map.subject_id)
                        .Distinct()
                        .ToListAsync(
                            cancellationToken);

                /*
                 * Find classrooms assigned to this section for the
                 * curriculum subjects.
                 */
                var validClassrooms =
                    await _context.Classrooms
                        .Where(
                            c =>
                                c.section_id ==
                                    section_id &&
                                requiredSubjectIds.Contains(
                                    c.subject_id))
                        .ToListAsync(
                            cancellationToken);

                foreach (
                    var classroom
                    in validClassrooms)
                {
                    bool alreadyEnrolled =
                        await _context.Enrollments
                            .AnyAsync(
                                e =>
                                    e.student_id ==
                                        newStudent.student_id &&
                                    e.classroom_id ==
                                        classroom.classroom_id,
                                cancellationToken);

                    if (!alreadyEnrolled)
                    {
                        _context.Enrollments.Add(
                            new Enrollment
                            {
                                student_id =
                                    newStudent.student_id,

                                classroom_id =
                                    classroom.classroom_id,

                                enrollment_date =
                                    DateTime.Now
                            });
                    }
                }

                /*
                 * Activation only occurs after Student creation and
                 * classroom enrollment have succeeded.
                 */
                pendingUser.status =
                    "Active";

                await _context.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "STUDENT_APPROVAL_SUCCESS: UserId={UserId}, StudentId={StudentId}, SectionId={SectionId}",
                    pendingUser.user_id,
                    newStudent.student_id,
                    section_id);

                TempData["SuccessMessage"] =
                    $"Student approved successfully. " +
                    $"Student number: {newStudent.student_number}.";

                return RedirectToAction(
                    nameof(RegisterStudent));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "STUDENT_APPROVAL_DATABASE_ERROR: UserId={UserId}",
                    user_id);

                TempData["ErrorMessage"] =
                    "Student approval failed because of a database constraint.";

                return RedirectToAction(
                    nameof(RegisterStudent));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "STUDENT_APPROVAL_FAILURE: UserId={UserId}",
                    user_id);

                TempData["ErrorMessage"] =
                    "Student approval failed.";

                return RedirectToAction(
                    nameof(RegisterStudent));
            }
        }

        // ============================================================
        // CURRICULUM
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MapSubjectToCurriculum(
            int strandId,
            int subjectId,
            string yearLevel,
            CancellationToken cancellationToken)
        {
            yearLevel =
                yearLevel?.Trim() ?? string.Empty;

            var strand =
                await _context.Strands
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        s =>
                            s.strand_id ==
                            strandId,
                        cancellationToken);

            bool subjectExists =
                await _context.Subjects.AnyAsync(
                    s =>
                        s.subject_id ==
                        subjectId,
                    cancellationToken);

            if (strand == null ||
                !subjectExists ||
                string.IsNullOrWhiteSpace(
                    strand.strand_code))
            {
                TempData["ErrorMessage"] =
                    "Invalid strand or subject.";

                return RedirectToAction(
                    nameof(ManageCurriculum));
            }

            var strandCode =
                strand.strand_code.Trim();

            bool exists =
                await _context.CurriculumMaps
                    .AnyAsync(
                        c =>
                            c.strand ==
                                strandCode &&
                            c.subject_id ==
                                subjectId &&
                            c.year_lvl ==
                                yearLevel,
                        cancellationToken);

            if (!exists)
            {
                var map =
                    new CurriculumMap
                    {
                        strand =
                            strandCode,

                        subject_id =
                            subjectId,

                        year_lvl =
                            yearLevel,

                        is_core_subject =
                            false
                    };

                _context.CurriculumMaps.Add(
                    map);

                try
                {
                    await _context.SaveChangesAsync(
                        cancellationToken);

                    TempData["SuccessMessage"] =
                        "Subject successfully added to the curriculum.";
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(
                        ex,
                        "CURRICULUM_MAPPING_DATABASE_ERROR");

                    TempData["ErrorMessage"] =
                        "The curriculum mapping could not be saved.";
                }
            }
            else
            {
                TempData["ErrorMessage"] =
                    "This subject is already mapped to that strand and year level.";
            }

            return RedirectToAction(
                nameof(ManageCurriculum));
        }

        [HttpGet]
        public async Task<IActionResult>
            GetSubjectsForCurriculum(
                int strandId,
                string yearLevel,
                CancellationToken cancellationToken)
        {
            var strand =
                await _context.Strands
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        s =>
                            s.strand_id ==
                            strandId,
                        cancellationToken);

            if (strand == null ||
                string.IsNullOrWhiteSpace(
                    strand.strand_code))
            {
                return BadRequest(
                    "Invalid strand.");
            }

            var strandCode =
                strand.strand_code.Trim();

            var subjects =
                await _context.CurriculumMaps
                    .AsNoTracking()
                    .Where(
                        c =>
                            c.strand ==
                                strandCode &&
                            c.year_lvl ==
                                yearLevel)
                    .Include(
                        c =>
                            c.Subject)
                    .Where(
                        c =>
                            c.Subject != null)
                    .Select(
                        c =>
                            new
                            {
                                id =
                                    c.Subject!.subject_id,

                                name =
                                    c.Subject!
                                        .subject_name
                            })
                    .OrderBy(
                        x => x.name)
                    .ToListAsync(
                        cancellationToken);

            return Json(
                subjects);
        }

        [HttpGet]
        public async Task<IActionResult>
            ManageCurriculum(
                CancellationToken cancellationToken)
        {
            var viewModel =
                new ManageCurriculumViewModel
                {
                    Strands =
                        await _context.Strands
                            .AsNoTracking()
                            .OrderBy(
                                s => s.strand_code)
                            .ToListAsync(
                                cancellationToken),

                    Subjects =
                        await _context.Subjects
                            .AsNoTracking()
                            .OrderBy(
                                s => s.subject_name)
                            .ToListAsync(
                                cancellationToken)
                };

            return View(
                viewModel);
        }
    }
}