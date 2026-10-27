using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AxertLMS.Data;

namespace AxertLMS.Controllers.Api
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/student/assignments")]
    public class StudentAssignmentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentAssignmentsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/student/assignments
        [HttpGet]
        public async Task<IActionResult> GetAssignments()
        {
            var assignments = await _context.Assignments
                .Include(a => a.Classroom)
                .OrderBy(a => a.due_date)
                .ToListAsync();

            return Ok(assignments);
        }

        // GET: api/student/assignments/{id}/submissions
        [HttpGet("{id}/submissions")]
        public async Task<IActionResult> GetAssignmentSubmissions(int id)
        {
            var submissions = await _context.Submissions
                .Where(s => s.assignment_id == id)
                .ToListAsync();

            return Ok(submissions);
        }
    }
}