using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AxertLMS.Data;
using AxertLMS.Models;

namespace AxertLMS.Controllers.Api
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/student/calendar")]
    public class StudentCalendarApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentCalendarApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/student/calendar
        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.Assignments.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(a => a.due_date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.due_date <= endDate.Value);
            }

            var calendarEvents = await query
                .Include(a => a.Classroom)
                .OrderBy(a => a.due_date)
                .Select(a => new
                {
                    AssignmentId = a.assignment_id,
                    Title = a.title,
                    DueDate = a.due_date,
                    Classroom = a.Classroom
                })
                .ToListAsync();

            return Ok(calendarEvents);
        }
    }
}