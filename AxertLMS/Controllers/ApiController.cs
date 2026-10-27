using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AxertLMS.Data;
using System.Linq;
using System.Threading.Tasks;

namespace AxertLMS.Controllers
{
    // The [ApiController] tag tells ASP.NET that this outputs raw JSON data, not HTML Views
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------
        // API ENDPOINT 1: Get System Stats
        // URL to test: /api/api/stats
        // ----------------------------------------------------
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalStudents = await _context.Students.CountAsync(),
                TotalSections = await _context.Sections.CountAsync(),
                Status = "Online",
                ServerLocation = "Davao City"
            };

            return Ok(stats); // Returns a standard HTTP 200 OK JSON response
        }

        // ----------------------------------------------------
        // API ENDPOINT 2: Get Active Academic Sections
        // URL to test: /api/api/sections
        // ----------------------------------------------------
        [HttpGet("sections")]
        public async Task<IActionResult> GetSections()
        {
            var sections = await _context.Sections
                .Include(s => s.Strand)
                .Select(s => new {
                    s.section_id,
                    s.section_name,
                    // ensure consistent anonymous type by making strand an object when substituting "N/A"
                    strand = s.Strand == null ? (object)"N/A" : s.Strand,
                    s.year_lvl
                })
                .ToListAsync();

            if (sections == null || !sections.Any())
            {
                return NotFound(new { message = "No sections found in the database." });
            }

            return Ok(sections);
        }
    }
}