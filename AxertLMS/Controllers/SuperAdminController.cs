using AxertLMS.Data;
using AxertLMS.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AxertLMS.Controllers
{
    [RoleGuard("Super Admin")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuperAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // Super Admins care about raw data weight and backend health
            ViewBag.TotalDbRows = await _context.Users.CountAsync() +
                                  await _context.Students.CountAsync() +
                                  await _context.Enrollments.CountAsync() +
                                  await _context.Submissions.CountAsync();

            ViewBag.ActiveAdmins = await _context.Users.CountAsync(u => u.role == "Admin");

            // Check if database connection is alive
            ViewBag.DbStatus = _context.Database.CanConnect() ? "ONLINE" : "OFFLINE";

            return View();
        }
        // ==========================================
        // SUPER ADMIN: ROLE MANAGER
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> RoleManager(string searchQuery)
        {
            var usersQuery = _context.Users.AsQueryable();

            // IT Fix: Added a search bar so they can actually find the user!
            if (!string.IsNullOrEmpty(searchQuery))
            {
                usersQuery = usersQuery.Where(u => u.l_name.Contains(searchQuery) ||
                                                   u.f_name.Contains(searchQuery) ||
                                                   u.email.Contains(searchQuery));
            }

            var users = await usersQuery.OrderBy(u => u.role).ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int user_id, string new_role)
        {
            var user = await _context.Users.FindAsync(user_id);
            if (user != null)
            {
                // Safety: Prevent Super Admin from accidentally demoting themselves and locking themselves out
                var currentSessionUserId = HttpContext.Session.GetInt32("UserId");
                if (user.user_id == currentSessionUserId)
                {
                    TempData["ErrorMessage"] = "CRITICAL: You cannot alter your own Super Admin access.";
                    return RedirectToAction("RoleManager");
                }

                user.role = new_role;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"System Access for {user.email} updated to [{new_role}].";
            }
            return RedirectToAction("RoleManager");
        }
        // ==========================================
        // SUPER ADMIN: RAW DATABASE MONITOR
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Database()
        {
            // Security Check: Ensure only Super Admins can access this raw data
            var currentRole = HttpContext.Session.GetString("UserRole");
            if (currentRole != "Super Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch metadata about the Database Connection
            ViewBag.DbProvider = _context.Database.ProviderName;
            ViewBag.CanConnect = _context.Database.CanConnect() ? "CONNECTED" : "DISCONNECTED";

            // Map out the exact row count of every table in the schema
            ViewBag.TableStats = new Dictionary<string, int>
            {
                { "Users", await _context.Users.CountAsync() },
                { "Students", await _context.Students.CountAsync() },
                { "Teachers", await _context.Teachers.CountAsync() },
                { "Sections", await _context.Sections.CountAsync() },
                { "Subjects", await _context.Subjects.CountAsync() },
                { "Classrooms", await _context.Classrooms.CountAsync() },
                { "Enrollments", await _context.Enrollments.CountAsync() },
                { "Announcements", await _context.Announcements.CountAsync() },
                { "Modules", await _context.Modules.CountAsync() },
                { "Assignments", await _context.Assignments.CountAsync() },
                { "Submissions", await _context.Submissions.CountAsync() },
                { "Grades", await _context.Grades.CountAsync() }
            };

            return View();
        }
    }
}