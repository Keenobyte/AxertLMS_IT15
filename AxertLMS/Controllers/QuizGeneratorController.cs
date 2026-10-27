using AxertLMS.Data;
using AxertLMS.Models;
using AxertLMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AxertLMS.Controllers
{
    public class QuizGeneratorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly InternalQuizGeneratorService _quizGenerator;
        private readonly ILogger<QuizGeneratorController> _logger;

        public QuizGeneratorController(
            ApplicationDbContext context,
            InternalQuizGeneratorService quizGenerator,
            ILogger<QuizGeneratorController> logger)
        {
            _context = context;
            _quizGenerator = quizGenerator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Classroom)
                    .ThenInclude(c => c!.Subject)
                .Include(q => q.Module)
                .Where(q =>
                    q.Classroom != null &&
                    q.Classroom.teacher_id == teacher.teacher_id)
                .OrderByDescending(q => q.created_at)
                .ToListAsync();

            return View(quizzes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateQuiz(
            int moduleId,
            string difficulty = "Easy")
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string[] allowedDifficulties =
            {
                "Easy",
                "Medium",
                "Hard"
            };

            if (!allowedDifficulties.Contains(
                difficulty,
                StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(difficulty),
                    "Please select a valid difficulty.");

                return BadRequest(ModelState);
            }

            var module = await _context.Modules
                .Include(m => m.Classroom)
                    .ThenInclude(c => c!.Subject)
                .FirstOrDefaultAsync(m => m.module_id == moduleId);

            if (module == null)
            {
                return NotFound("The requested module was not found.");
            }

            if (module.Classroom == null ||
                module.Classroom.teacher_id != teacher.teacher_id)
            {
                return Forbid();
            }

            List<QuizQuestionDto> generatedQuestions =
                _quizGenerator.Generate(
                    module.title,
                    module.description ?? string.Empty,
                    difficulty);

            if (generatedQuestions.Count == 0)
            {
                return BadRequest(
                    "The quiz generator did not produce any questions.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var quiz = new Quiz
                {
                    classroom_id = module.classroom_id,
                    module_id = module.module_id,
                    title = $"{module.title} Quiz",
                    difficulty = difficulty,
                    created_at = DateTime.UtcNow
                };

                _context.Quizzes.Add(quiz);

                await _context.SaveChangesAsync();

                foreach (var question in generatedQuestions)
                {
                    _context.QuizQuestions.Add(new QuizQuestion
                    {
                        quiz_id = quiz.quiz_id,
                        question_type = question.question_type,
                        question_text = question.question_text,
                        option_a = question.option_a,
                        option_b = question.option_b,
                        option_c = question.option_c,
                        option_d = question.option_d,
                        correct_answer = question.correct_answer
                    });
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Teacher {TeacherId} generated quiz {QuizId} for module {ModuleId}.",
                    teacher.teacher_id,
                    quiz.quiz_id,
                    module.module_id);

                return RedirectToAction(
                    nameof(QuizDetails),
                    new { id = quiz.quiz_id });
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    exception,
                    "Failed to generate a quiz for module {ModuleId}.",
                    moduleId);

                return Problem(
                    title: "Quiz generation failed",
                    detail: "The quiz could not be saved. Please try again.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        public async Task<IActionResult> QuizDetails(int id)
        {
            var teacher = await GetCurrentTeacherAsync();

            if (teacher == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                .Include(q => q.Module)
                .Include(q => q.Classroom)
                    .ThenInclude(c => c!.Subject)
                .FirstOrDefaultAsync(q => q.quiz_id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            if (quiz.Classroom == null ||
                quiz.Classroom.teacher_id != teacher.teacher_id)
            {
                return Forbid();
            }

            return View(quiz);
        }

        private async Task<Teacher?> GetCurrentTeacherAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            string? role = HttpContext.Session.GetString("UserRole");

            if (!userId.HasValue || role != "Teacher")
            {
                return null;
            }

            return await _context.Teachers
                .FirstOrDefaultAsync(t => t.user_id == userId.Value);
        }
    }
}