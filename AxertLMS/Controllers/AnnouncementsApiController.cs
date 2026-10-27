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
    [Route("api/announcements")]
    public class AnnouncementsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/announcements
        [HttpGet]
        public async Task<IActionResult> GetAnnouncements([FromQuery] int count = 10)
        {
            var announcements = await _context.Announcements
                .OrderByDescending(a => a.created_at)
                .Take(count)
                .ToListAsync();

            return Ok(announcements);
        }
    }
}