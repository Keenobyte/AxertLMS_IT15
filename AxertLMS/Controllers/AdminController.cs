using AxertLMS.Data;
using AxertLMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AxertLMS.Helpers;

namespace AxertLMS.Controllers
{
    // ONLY Admins and Super Admins can access ANY method in this file now.
    // If a Student tries to type /Admin/Dashboard, the server rejects it instantly.
    [RoleGuard("Admin", "Super Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ADMIN DASHBOARD
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // 1. Existing System Metrics
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.ActiveUsers = await _context.Users.CountAsync(u => u.status == "Active");
            ViewBag.SuspendedUsers = await _context.Users.CountAsync(u => u.status == "Inactive");

            ViewBag.AdminCount = await _context.Users.CountAsync(u => u.role == "Admin" || u.role == "Super Admin");
            ViewBag.RegistrarCount = await _context.Users.CountAsync(u => u.role == "Registrar");
            ViewBag.TeacherCount = await _context.Users.CountAsync(u => u.role == "Teacher");
            ViewBag.StudentCount = await _context.Users.CountAsync(u => u.role == "Student");

            // ==========================================
            // PROFESSIONAL API INTEGRATION
            // ==========================================
            using var client = new HttpClient();

            // API 1: Network Security / IP Geolocation API (ip-api.com)
            try
            {
                var ipJson = await client.GetStringAsync("http://ip-api.com/json/");
                using var ipDoc = JsonDocument.Parse(ipJson);

                ViewBag.AdminIP = ipDoc.RootElement.GetProperty("query").GetString();
                ViewBag.AdminISP = ipDoc.RootElement.GetProperty("isp").GetString();
                ViewBag.AdminCity = ipDoc.RootElement.GetProperty("city").GetString();
            }
            catch
            {
                ViewBag.AdminIP = "Unknown";
                ViewBag.AdminISP = "Connection Error";
                ViewBag.AdminCity = "Unknown";
            }

            // API 2: Philippine Public Holidays API (Nager.Date)
            try
            {
                // Fetch all PH holidays for the current year
                int year = DateTime.Now.Year;
                var holidayJson = await client.GetStringAsync($"https://date.nager.at/api/v3/PublicHolidays/{year}/PH");
                using var holidayDoc = JsonDocument.Parse(holidayJson);

                // Find the NEXT upcoming holiday safely
                var nextHoliday = holidayDoc.RootElement.EnumerateArray()
                    .FirstOrDefault(h =>
                        h.TryGetProperty("date", out var dateProp) &&
                        DateTime.TryParse(dateProp.GetString(), out var parsedDate) &&
                        parsedDate >= DateTime.Now
                    );

                if (nextHoliday.ValueKind != JsonValueKind.Undefined)
                {
                    ViewBag.NextHolidayName = nextHoliday.GetProperty("localName").GetString();

                    if (DateTime.TryParse(nextHoliday.GetProperty("date").GetString(), out var holidayDate))
                    {
                        ViewBag.NextHolidayDate = holidayDate.ToString("MMMM dd, yyyy");
                    }
                    else
                    {
                        ViewBag.NextHolidayDate = "--";
                    }
                }
                else
                {
                    ViewBag.NextHolidayName = "No upcoming holidays";
                    ViewBag.NextHolidayDate = "--";
                }
            }
            catch
            {
                ViewBag.NextHolidayName = "API Offline";
                ViewBag.NextHolidayDate = "--";
            }

            return View();
        } // <-- Fixed! This closing brace and return statement are now restored.

        // ==========================================
        // 2. MANAGE USERS (Global List)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            // Fetch ALL users, ordered by ID
            var allUsers = await _context.Users
                                         .OrderBy(u => u.user_id)
                                         .ToListAsync();
            return View(allUsers);
        }

        // ==========================================
        // 3. THE "KILL SWITCH" (Toggle Active/Inactive)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int user_id)
        {
            var user = await _context.Users.FindAsync(user_id);
            if (user != null)
            {
                // IT SECURITY FIX: Admins cannot touch Super Admins
                if (user.role == "Super Admin")
                {
                    TempData["ErrorMessage"] = "ACCESS DENIED: You do not have clearance to alter a Super Admin account.";
                    return RedirectToAction("ManageUsers");
                }

                user.status = (user.status == "Active") ? "Inactive" : "Active";
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"User {user.email} status changed to {user.status}.";
            }
            return RedirectToAction("ManageUsers");
        }

    }
}