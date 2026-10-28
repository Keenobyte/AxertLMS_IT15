using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AxertLMS.Data;
using AxertLMS.Helpers;
using AxertLMS.Models;
using AxertLMS.Services;
using GoogleTokenResponse = AxertLMS.Models.GoogleTokenResponse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AxertLMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly GoogleOAuthService _googleOAuth;
        private readonly GoogleTokenStore _googleTokens;

        private static readonly Regex EmailRegex =
            new(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant);

        private static readonly Regex GmailRegex =
            new(
                @"^[^@\s]+@gmail\.com$",
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

        public AccountController(
            ApplicationDbContext context,
            ILogger<AccountController> logger,
            GoogleOAuthService googleOAuth,
            GoogleTokenStore googleTokens)
        {
            _context = context;
            _logger = logger;
            _googleOAuth = googleOAuth;
            _googleTokens = googleTokens;
        }

        // ============================================================
        // REGISTER PAGE
        // ============================================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
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
                    GoogleOAuthService.OnboardingUserIdSessionKey);

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
                    GoogleOAuthService.OnboardingUserIdSessionKey);

                return RedirectToAction(nameof(Login));
            }

            if (string.Equals(
                    user.status,
                    "Inactive",
                    StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.Remove(
                    GoogleOAuthService.OnboardingUserIdSessionKey);

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
            User model,
            string rawPassword,
            CancellationToken cancellationToken)
        {
            if (
                model == null ||
                string.IsNullOrWhiteSpace(rawPassword))
            {
                ViewBag.Error =
                    "Registration inputs cannot be empty.";

                return View();
            }

            model.email =
                model.email?
                    .Trim()
                    .ToLowerInvariant()
                ?? string.Empty;

            if (!EmailRegex.IsMatch(model.email))
            {
                _logger.LogWarning(
                    "VALIDATION_FAILURE: Malformed registration email.");

                ViewBag.Error =
                    "Invalid email formatting provided.";

                return View();
            }

            if (!GmailRegex.IsMatch(model.email))
            {
                ViewBag.Error =
                    "Please use a Gmail address.";

                return View();
            }

            var existingUser =
                await _context.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        u => u.email == model.email,
                        cancellationToken);

            if (existingUser != null)
            {
                ViewBag.Error =
                    "An account with this email already exists.";

                return View();
            }

            try
            {
                var newUser =
                    new User
                    {
                        f_name =
                            model.f_name.Trim(),

                        l_name =
                            model.l_name.Trim(),

                        email =
                            model.email,

                        password =
                            PasswordHelper.Hash(
                                rawPassword),

                        role =
                            "Student",

                        status =
                            "Pending"
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
                _logger.LogWarning(
                    ex,
                    "REGISTRATION_DATABASE_CONFLICT");

                ViewBag.Error =
                    "This email is already associated with an account.";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "REGISTRATION_FAILURE");

                ViewBag.Error =
                    "An internal error occurred. Please try again later.";

                return View();
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

            var user =
                await _context.Users
                    .SingleOrDefaultAsync(
                        u => u.email == Email,
                        cancellationToken);

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

        // ============================================================
        // GOOGLE LOGIN
        // ============================================================

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            try
            {
                return BeginGoogleAuthorization(
                    "login",
                    null,
                    false);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_CONFIGURATION_ERROR");

                TempData["Error"] =
                    "Google sign-in is not configured correctly.";

                return RedirectToAction(nameof(Login));
            }
        }

        // ============================================================
        // GOOGLE REGISTRATION
        // ============================================================

        [HttpGet]
        public IActionResult GoogleRegister()
        {
            try
            {
                return BeginGoogleAuthorization(
                    "register",
                    null,
                    false);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_CONFIGURATION_ERROR");

                TempData["Error"] =
                    "Google registration is not configured correctly.";

                return RedirectToAction(nameof(Register));
            }
        }

        // ============================================================
        // GOOGLE CALENDAR CONNECTION
        // Existing route retained for the Calendar UI.
        // ============================================================

        [HttpGet]
        [RoleGuard(
            "Student",
            "Teacher",
            "Registrar",
            "Admin",
            "Super Admin")]
        public IActionResult GoogleCalendarConnect()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            try
            {
                return BeginGoogleAuthorization(
                    "calendar",
                    userId.Value,
                    true);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_CONFIGURATION_ERROR for Calendar connection");

                TempData["Error"] =
                    "Google Calendar is not configured correctly.";

                return RedirectToAction(
                    "Index",
                    "GoogleCalendar");
            }
        }

        // ============================================================
        // GOOGLE CALLBACK
        // Existing /Account/GoogleCallback route retained.
        // ============================================================

        [HttpGet]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GoogleCallback(
            string? code,
            string? state,
            string? error,
            string? error_description,
            CancellationToken cancellationToken)
        {
            string? purpose =
                HttpContext.Session.GetString(
                    GoogleOAuthService.IntentSessionKey);

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning(
                    "GOOGLE_OAUTH_DENIED: {Error}",
                    error);

                string message =
                    string.IsNullOrWhiteSpace(error_description)
                        ? "Google authorization was cancelled."
                        : error_description;

                ClearGoogleOAuthSession();
                TempData["Error"] = message;

                return RedirectAfterGoogleOAuth(purpose);
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning(
                    "GOOGLE_OAUTH_CALLBACK_MISSING_CODE");

                ClearGoogleOAuthSession();
                TempData["Error"] =
                    "Google did not return an authorization code. " +
                    "Please start the Google sign-in process again.";

                return RedirectAfterGoogleOAuth(purpose);
            }

            string? expectedState =
                HttpContext.Session.GetString(
                    GoogleOAuthService.StateSessionKey);

            string? codeVerifier =
                HttpContext.Session.GetString(
                    GoogleOAuthService.CodeVerifierSessionKey);

            string? timestampValue =
                HttpContext.Session.GetString(
                    GoogleOAuthService.TimestampSessionKey);

            string? storedUserId =
                HttpContext.Session.GetString(
                    GoogleOAuthService.UserIdSessionKey);

            if (
                string.IsNullOrWhiteSpace(expectedState) ||
                string.IsNullOrWhiteSpace(codeVerifier) ||
                string.IsNullOrWhiteSpace(timestampValue) ||
                string.IsNullOrWhiteSpace(purpose))
            {
                _logger.LogWarning(
                    "GOOGLE_OAUTH_CALLBACK_SESSION_MISSING");

                ClearGoogleOAuthSession();
                TempData["Error"] =
                    "Your Google sign-in session expired. Please start again.";

                return RedirectAfterGoogleOAuth(purpose);
            }

            bool stateValid =
                CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedState),
                    Encoding.UTF8.GetBytes(state ?? string.Empty));

            if (!stateValid)
            {
                _logger.LogWarning(
                    "GOOGLE_OAUTH_CALLBACK_STATE_MISMATCH");

                ClearGoogleOAuthSession();
                TempData["Error"] =
                    "Google sign-in could not be verified. Please start again.";

                return RedirectAfterGoogleOAuth(purpose);
            }

            if (
                !DateTimeOffset.TryParse(
                    timestampValue,
                    out DateTimeOffset createdAt))
            {
                ClearGoogleOAuthSession();
                TempData["Error"] =
                    "Google sign-in session is invalid.";

                return RedirectAfterGoogleOAuth(purpose);
            }

            TimeSpan age =
                DateTimeOffset.UtcNow - createdAt;

            if (
                age < TimeSpan.Zero ||
                age > TimeSpan.FromMinutes(
                    GoogleOAuthService.StateLifetimeMinutes))
            {
                ClearGoogleOAuthSession();
                TempData["Error"] =
                    "Google sign-in session expired. Please try again.";

                return RedirectAfterGoogleOAuth(purpose);
            }

            ClearGoogleOAuthSession();

            try
            {
                GoogleTokenResponse token =
                    await _googleOAuth.ExchangeCodeAsync(
                        code,
                        codeVerifier,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(token.AccessToken))
                {
                    TempData["Error"] =
                        "Google did not return a valid access token.";

                    return RedirectAfterGoogleOAuth(purpose);
                }

                GoogleUserInfo googleUser =
                    await _googleOAuth.GetUserInfoAsync(
                        token.AccessToken,
                        cancellationToken);

                if (
                    string.IsNullOrWhiteSpace(googleUser.Subject) ||
                    string.IsNullOrWhiteSpace(googleUser.Email))
                {
                    TempData["Error"] =
                        "Google did not return a valid user identity.";

                    return RedirectAfterGoogleOAuth(purpose);
                }

                if (!googleUser.EmailVerified)
                {
                    TempData["Error"] =
                        "Your Google email address is not verified.";

                    return RedirectAfterGoogleOAuth(purpose);
                }

                if (!GmailRegex.IsMatch(googleUser.Email))
                {
                    TempData["Error"] =
                        "AxertLMS currently accepts personal Gmail accounts only.";

                    return RedirectAfterGoogleOAuth(purpose);
                }

                string email =
                    googleUser.Email.Trim().ToLowerInvariant();

                GoogleAccount? googleAccount =
                    await _googleTokens.GetBySubjectAsync(
                        googleUser.Subject,
                        cancellationToken);

                User? user =
                    googleAccount?.User;

                if (user == null)
                {
                    user =
                        await _context.Users
                            .SingleOrDefaultAsync(
                                u => u.email == email,
                                cancellationToken);
                }

                switch (purpose)
                {
                    case "register":
                        if (user != null)
                        {
                            return await ProcessGoogleLoginAsync(
                                user,
                                googleUser,
                                token,
                                googleAccount,
                                cancellationToken);
                        }

                        return await ProcessGoogleRegistrationAsync(
                            user,
                            googleUser,
                            token,
                            cancellationToken);

                    case "login":
                        if (user == null)
                        {
                            return await ProcessGoogleRegistrationAsync(
                                null,
                                googleUser,
                                token,
                                cancellationToken);
                        }

                        return await ProcessGoogleLoginAsync(
                            user,
                            googleUser,
                            token,
                            googleAccount,
                            cancellationToken);

                    case "calendar":
                        return await ProcessGoogleCalendarConnectionAsync(
                            storedUserId,
                            user,
                            googleUser,
                            token,
                            googleAccount,
                            cancellationToken);

                    default:
                        TempData["Error"] =
                            "Invalid Google OAuth request.";

                        return RedirectToAction(nameof(Login));
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_OAUTH_HTTP_FAILURE");

                TempData["Error"] =
                    "Could not communicate with Google. Please try again.";

                return RedirectAfterGoogleOAuth(purpose);
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_OAUTH_JSON_FAILURE");

                TempData["Error"] =
                    "Google returned an unexpected response.";

                return RedirectAfterGoogleOAuth(purpose);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "GOOGLE_OAUTH_OPERATION_FAILURE");

                TempData["Error"] =
                    ex.Message;

                return RedirectAfterGoogleOAuth(purpose);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_OAUTH_DATABASE_FAILURE");

                TempData["Error"] =
                    "Google authentication could not update your AxertLMS account. " +
                    "Please try again.";

                return RedirectAfterGoogleOAuth(purpose);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_OAUTH_CALLBACK_FAILURE");

                TempData["Error"] =
                    "Google authentication could not be completed.";

                return RedirectAfterGoogleOAuth(purpose);
            }
        }

        // ============================================================
        // GOOGLE REGISTRATION PROCESSING
        // ============================================================

        private async Task<IActionResult>
            ProcessGoogleRegistrationAsync(
                User? existingUser,
                GoogleUserInfo googleUser,
                GoogleTokenResponse token,
                CancellationToken cancellationToken)
        {
            if (existingUser != null)
            {
                TempData["Error"] =
                    "An AxertLMS account already exists for this Gmail address. " +
                    "Use Sign in with Google instead.";

                return RedirectToAction(nameof(Login));
            }

            var existingGoogleAccount =
                await _googleTokens.GetBySubjectAsync(
                    googleUser.Subject!,
                    cancellationToken);

            if (existingGoogleAccount != null)
            {
                TempData["Error"] =
                    "This Google account has already been registered.";

                return RedirectToAction(nameof(Login));
            }

            string firstName =
                LimitName(
                    googleUser.GivenName ??
                    ExtractFirstName(googleUser.Name));

            string lastName =
                LimitName(
                    googleUser.FamilyName ??
                    ExtractLastName(googleUser.Name));

            string generatedPassword =
                Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(48));

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                var newUser =
                    new User
                    {
                        f_name = firstName,
                        l_name = lastName,
                        email = googleUser.Email!
                            .Trim()
                            .ToLowerInvariant(),
                        password =
                            PasswordHelper.Hash(
                                generatedPassword),
                        role = "Student",
                        status = "Pending"
                    };

                _context.Users.Add(newUser);

                await _context.SaveChangesAsync(
                    cancellationToken);

                await _googleTokens.SaveAsync(
                    newUser.user_id,
                    googleUser,
                    token,
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "GOOGLE_REGISTRATION_SUCCESS: UserId={UserId}",
                    newUser.user_id);

                TempData["RegistrationSuccess"] =
                    "Your Google account has been verified and your enrollment application has been submitted. " +
                    "The Registrar must approve your application before you can access the LMS.";

                BeginStudentOnboarding(newUser.user_id);

                return RedirectToAction(
                    nameof(StudentStartHere));
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }

        // ============================================================
        // GOOGLE LOGIN PROCESSING
        // ============================================================

        private async Task<IActionResult>
            ProcessGoogleLoginAsync(
                User? user,
                GoogleUserInfo googleUser,
                GoogleTokenResponse token,
                GoogleAccount? googleAccount,
                CancellationToken cancellationToken)
        {
            if (user == null)
            {
                TempData["Error"] =
                    "No AxertLMS account exists for this Google account. " +
                    "Register first.";

                return RedirectToAction(nameof(Login));
            }

            if (
                googleAccount != null &&
                googleAccount.UserId != user.user_id)
            {
                TempData["Error"] =
                    "This Google account is linked to another AxertLMS account.";

                return RedirectToAction(nameof(Login));
            }

            string normalizedEmail =
                googleUser.Email!
                    .Trim()
                    .ToLowerInvariant();

            if (!string.Equals(
                    user.email,
                    normalizedEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "The Google email does not match the AxertLMS account.";

                return RedirectToAction(nameof(Login));
            }

            await _googleTokens.SaveAsync(
                user.user_id,
                googleUser,
                token,
                cancellationToken);

            if (
                string.Equals(
                    user.status,
                    "Inactive",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Your AxertLMS account is currently inactive.";

                return RedirectToAction(nameof(Login));
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

            if (
                string.Equals(
                    user.status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Your enrollment application is still pending Registrar approval.";

                return RedirectToAction(nameof(Login));
            }

            SignInUser(user);

            _logger.LogInformation(
                "GOOGLE_LOGIN_SUCCESS: UserId={UserId}",
                user.user_id);

            return RedirectByRole(user.role);
        }

        // ============================================================
        // GOOGLE CALENDAR CONNECTION PROCESSING
        // ============================================================

        private async Task<IActionResult>
            ProcessGoogleCalendarConnectionAsync(
                string? storedUserId,
                User? resolvedUser,
                GoogleUserInfo googleUser,
                GoogleTokenResponse token,
                GoogleAccount? googleAccount,
                CancellationToken cancellationToken)
        {
            if (!int.TryParse(
                    storedUserId,
                    out int userId))
            {
                TempData["Error"] =
                    "Your AxertLMS session could not be verified. Please sign in again.";

                return RedirectToAction(nameof(Login));
            }

            User? currentUser =
                await _context.Users
                    .SingleOrDefaultAsync(
                        u => u.user_id == userId,
                        cancellationToken);

            if (currentUser == null)
            {
                TempData["Error"] =
                    "Your AxertLMS account could not be found. Please sign in again.";

                return RedirectToAction(nameof(Login));
            }

            if (
                resolvedUser != null &&
                resolvedUser.user_id != userId)
            {
                TempData["Error"] =
                    "The Google account is associated with a different AxertLMS account.";

                return RedirectToAction(
                    "Index",
                    "GoogleCalendar");
            }

            if (
                googleAccount != null &&
                googleAccount.UserId != userId)
            {
                TempData["Error"] =
                    "This Google account is already connected to another AxertLMS account.";

                return RedirectToAction(
                    "Index",
                    "GoogleCalendar");
            }

            string normalizedEmail =
                googleUser.Email!
                    .Trim()
                    .ToLowerInvariant();

            if (!string.Equals(
                    currentUser.email,
                    normalizedEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Connect the same Google account that matches your AxertLMS Gmail address.";

                return RedirectToAction(
                    "Index",
                    "GoogleCalendar");
            }

            await _googleTokens.SaveAsync(
                userId,
                googleUser,
                token,
                cancellationToken);

            TempData["GoogleConnected"] =
                "Google Calendar has been connected successfully.";

            return RedirectToAction(
                "Index",
                "GoogleCalendar");
        }

        // ============================================================
        // BEGIN GOOGLE AUTHORIZATION
        // ============================================================

        private IActionResult BeginGoogleAuthorization(
            string purpose,
            int? userId,
            bool includeCalendar)
        {
            string state =
                GoogleOAuthService.GenerateState();

            string codeVerifier =
                GoogleOAuthService.GenerateCodeVerifier();

            string authorizationUrl =
                _googleOAuth.CreateAuthorizationUrl(
                    state,
                    codeVerifier,
                    includeCalendar);

            HttpContext.Session.SetString(
                GoogleOAuthService.StateSessionKey,
                state);

            HttpContext.Session.SetString(
                GoogleOAuthService.CodeVerifierSessionKey,
                codeVerifier);

            HttpContext.Session.SetString(
                GoogleOAuthService.IntentSessionKey,
                purpose);

            HttpContext.Session.SetString(
                GoogleOAuthService.TimestampSessionKey,
                DateTimeOffset.UtcNow.ToString("O"));

            if (userId.HasValue)
            {
                HttpContext.Session.SetString(
                    GoogleOAuthService.UserIdSessionKey,
                    userId.Value.ToString());
            }
            else
            {
                HttpContext.Session.Remove(
                    GoogleOAuthService.UserIdSessionKey);
            }

            return Redirect(authorizationUrl);
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
                GoogleOAuthService.OnboardingUserIdSessionKey,
                userId);
        }

        private void ClearGoogleOAuthSession()
        {
            HttpContext.Session.Remove(
                GoogleOAuthService.StateSessionKey);

            HttpContext.Session.Remove(
                GoogleOAuthService.CodeVerifierSessionKey);

            HttpContext.Session.Remove(
                GoogleOAuthService.IntentSessionKey);

            HttpContext.Session.Remove(
                GoogleOAuthService.TimestampSessionKey);

            HttpContext.Session.Remove(
                GoogleOAuthService.UserIdSessionKey);
        }

        private IActionResult RedirectAfterGoogleOAuth(
            string? purpose)
        {
            return purpose switch
            {
                "register" =>
                    RedirectToAction(nameof(Register)),

                "calendar" =>
                    RedirectToAction(
                        "Index",
                        "GoogleCalendar"),

                _ =>
                    RedirectToAction(nameof(Login))
            };
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
