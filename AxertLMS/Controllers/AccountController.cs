using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AxertLMS.Data;
using AxertLMS.Helpers;
using AxertLMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AxertLMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private const string StudentOnboardingUserIdSessionKey =
            "AxertLMS.StudentOnboarding.UserId";

        private static readonly Regex EmailRegex =
            new(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant);

        public AccountController(
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // REGISTER PAGE
        // ============================================================

        [HttpGet]
        public IActionResult Register()
        {
            return View(new StudentRegistrationViewModel());
        }

        [HttpGet]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> StudentStartHere(
            CancellationToken cancellationToken)
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    StudentOnboardingUserIdSessionKey);

            if (!userId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            User? user =
                await _context.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        u => u.user_id == userId.Value,
                        cancellationToken);

            if (user == null ||
                !string.Equals(
                    user.role,
                    "Student",
                    StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.Remove(
                    StudentOnboardingUserIdSessionKey);

                return RedirectToAction(nameof(Login));
            }

            if (string.Equals(
                    user.status,
                    "Inactive",
                    StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.Remove(
                    StudentOnboardingUserIdSessionKey);

                TempData["Error"] =
                    "Your account is currently inactive. Please contact the Registrar.";

                return RedirectToAction(nameof(Login));
            }

            bool hasStudentProfile =
                await _context.Students
                    .AsNoTracking()
                    .AnyAsync(
                        s => s.user_id == user.user_id,
                        cancellationToken);

            if (hasStudentProfile &&
                string.Equals(
                    user.status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                SignInUser(user);
                return RedirectByRole(user.role);
            }

            ViewBag.StudentName = user.f_name;
            ViewBag.IsAwaitingApproval =
                string.Equals(
                    user.status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase);
            ViewBag.HasStudentProfile = hasStudentProfile;

            return View();
        }

        // ============================================================
        // NORMAL PASSWORD REGISTRATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            StudentRegistrationViewModel model,
            CancellationToken cancellationToken)
        {
            if (model == null)
            {
                return BadRequest();
            }

            model.FirstName =
                model.FirstName?.Trim() ?? string.Empty;
            model.LastName =
                model.LastName?.Trim() ?? string.Empty;
            model.Email =
                model.Email?.Trim().ToLowerInvariant()
                ?? string.Empty;

            ModelState.Clear();
            if (!TryValidateModel(model))
            {
                return View(model);
            }

            try
            {
                bool emailExists =
                    await _context.Users
                        .AsNoTracking()
                        .AnyAsync(
                            u => u.email == model.Email,
                            cancellationToken);

                if (emailExists)
                {
                    ViewBag.Error =
                        "An account with this email already exists.";

                    return View(model);
                }

                var newUser =
                    new User
                    {
                        f_name = model.FirstName,
                        l_name = model.LastName,
                        email = model.Email,
                        password = PasswordHelper.Hash(model.Password),
                        role = "Student",
                        status = "Pending"
                    };

                _context.Users.Add(newUser);

                await _context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "REGISTRATION_SUCCESS: Pending student created. UserId={UserId}",
                    newUser.user_id);

                TempData["RegistrationSuccess"] =
                    "Your enrollment application has been submitted. " +
                    "Please wait for Registrar approval.";

                return RedirectToAction(nameof(Login));
            }
            catch (DbUpdateException ex)
            {
                SqlException? sqlException =
                    ex.GetBaseException() as SqlException;

                if (sqlException?.Number is 2601 or 2627)
                {
                    ViewBag.Error =
                        "An account with this email already exists.";
                }
                else
                {
                    _logger.LogError(
                        ex,
                        "Manual student registration database failure.");

                    ViewBag.Error =
                        "Your application could not be submitted. Please try again later.";
                }

                return View(model);
            }
            catch (Exception ex) when (ContainsSqlException(ex))
            {
                _logger.LogError(
                    ex,
                    "Database connection failed during manual student registration.");

                ViewBag.Error =
                    "Registration is temporarily unavailable. Please try again later.";

                return View(model);
            }
        }

        // ============================================================
        // LOGIN PAGE
        // ============================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ============================================================
        // NORMAL PASSWORD LOGIN
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string Email,
            string Password,
            CancellationToken cancellationToken)
        {
            await Task.Delay(500, cancellationToken);

            Email =
                Email?
                    .Trim()
                    .ToLowerInvariant()
                ?? string.Empty;

            Password ??= string.Empty;

            if (
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.Error =
                    "Please complete all mandatory login fields.";

                return View();
            }

            if (!EmailRegex.IsMatch(Email))
            {
                ViewBag.Error =
                    "Invalid email or password. Please try again.";

                return View();
            }

            User? user;

            try
            {
                user =
                    await _context.Users
                        .SingleOrDefaultAsync(
                            u => u.email == Email,
                            cancellationToken);
            }
            catch (Exception ex) when (ContainsSqlException(ex))
            {
                _logger.LogError(
                    ex,
                    "Database connection failed during login.");

                Response.StatusCode =
                    StatusCodes.Status503ServiceUnavailable;

                ViewBag.Error =
                    "Sign-in is temporarily unavailable. Please try again later.";

                return View();
            }

            if (user == null)
            {
                ViewBag.Error =
                    "Invalid email or password. Please try again.";

                return View();
            }

            bool isPasswordValid = false;
            bool needsBcryptUpgrade = false;

            if (
                !string.IsNullOrWhiteSpace(user.password) &&
                user.password.Length == 64 &&
                !user.password.StartsWith("$"))
            {
                string hashedInput =
                    PasswordHelper.HashSHA256(Password);

                isPasswordValid =
                    CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(hashedInput),
                        Encoding.UTF8.GetBytes(user.password));

                needsBcryptUpgrade = isPasswordValid;
            }
            else
            {
                isPasswordValid =
                    PasswordHelper.VerifyBCrypt(
                        Password,
                        user.password,
                        _logger);
            }

            if (!isPasswordValid)
            {
                ViewBag.Error =
                    "Invalid email or password. Please try again.";

                return View();
            }

            if (needsBcryptUpgrade)
            {
                user.password =
                    PasswordHelper.Hash(Password);

                await _context.SaveChangesAsync(
                    cancellationToken);
            }

            if (
                string.Equals(
                    user.status,
                    "Inactive",
                    StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error =
                    "Your account is currently inactive. " +
                    "Please contact the Registrar.";

                return View();
            }

            if (string.Equals(
                    user.role,
                    "Student",
                    StringComparison.OrdinalIgnoreCase))
            {
                bool hasStudentProfile =
                    await _context.Students
                        .AsNoTracking()
                        .AnyAsync(
                            s => s.user_id == user.user_id,
                            cancellationToken);

                if (string.Equals(
                        user.status,
                        "Pending",
                        StringComparison.OrdinalIgnoreCase) ||
                    !hasStudentProfile)
                {
                    BeginStudentOnboarding(user.user_id);
                    return RedirectToAction(
                        nameof(StudentStartHere));
                }
            }

            if (string.Equals(
                    user.status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error =
                    "Your enrollment application is still pending Registrar approval.";

                return View();
            }

            SignInUser(user);

            _logger.LogInformation(
                "AUTH_SUCCESS: Password login completed. UserId={UserId}",
                user.user_id);

            return RedirectByRole(user.role);
        }

        private static bool ContainsSqlException(Exception exception)
        {
            Exception? current = exception;

            while (current != null)
            {
                if (current is SqlException)
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }

        // ============================================================
        // SESSION
        // ============================================================

        private void SignInUser(User user)
        {
            HttpContext.Session.Clear();

            HttpContext.Session.SetInt32(
                "UserId",
                user.user_id);

            HttpContext.Session.SetString(
                "UserRole",
                user.role);

            HttpContext.Session.SetString(
                "UserEmail",
                user.email);
        }

        private void BeginStudentOnboarding(int userId)
        {
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("UserRole");
            HttpContext.Session.Remove("UserEmail");

            HttpContext.Session.SetInt32(
                StudentOnboardingUserIdSessionKey,
                userId);
        }

        // ============================================================
        // ROLE REDIRECTION
        // ============================================================

        private IActionResult RedirectByRole(string role)
        {
            return role switch
            {
                "Teacher" =>
                    RedirectToAction(
                        "Dashboard",
                        "Teacher"),

                "Student" =>
                    RedirectToAction(
                        "Dashboard",
                        "Student"),

                "Registrar" =>
                    RedirectToAction(
                        "Dashboard",
                        "Registrar"),

                "Admin" =>
                    RedirectToAction(
                        "Dashboard",
                        "Admin"),

                "Super Admin" =>
                    RedirectToAction(
                        "Dashboard",
                        "SuperAdmin"),

                _ =>
                    RedirectToAction(
                        "Index",
                        "Home")
            };
        }

        // ============================================================
        // NAME HELPERS
        // ============================================================

        private static string LimitName(string? value)
        {
            string result =
                string.IsNullOrWhiteSpace(value)
                    ? "Student"
                    : value.Trim();

            return result.Length <= 50
                ? result
                : result[..50];
        }

        private static string ExtractFirstName(
            string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "Student";
            }

            string[] parts =
                fullName
                    .Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 0
                ? parts[0]
                : "Student";
        }

        private static string ExtractLastName(
            string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "Student";
            }

            string[] parts =
                fullName
                    .Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 1
                ? parts[^1]
                : "Student";
        }

        // ============================================================
        // ACCESS DENIED
        // ============================================================

        [HttpGet]
        public IActionResult AccessDenied(
            string? controllerName,
            string? actionName,
            string? requiredRoles)
        {
            ViewBag.ControllerName =
                controllerName ??
                "requested resource";

            ViewBag.ActionName =
                actionName ??
                "requested action";

            ViewBag.RequiredRoles =
                requiredRoles ??
                "authorized role";

            ViewBag.CurrentRole =
                HttpContext.Session.GetString(
                    "UserRole") ??
                "Unauthenticated";

            return View();
        }

        // ============================================================
        // LOGOUT
        // ============================================================

        [HttpGet]
        public IActionResult Logout()
        {
            _logger.LogInformation(
                "AUTH_TERMINATION: User logout.");

            HttpContext.Session.Clear();

            Response.Cookies.Delete(
                "AxertLMS.Session");

            return RedirectToAction(nameof(Login));
        }
    }
}
