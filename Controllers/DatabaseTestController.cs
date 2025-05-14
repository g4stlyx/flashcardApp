using System.Linq;
using Microsoft.AspNetCore.Mvc;
using flashcardApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Text.Json;

namespace flashcardApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseTestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DatabaseTestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/DatabaseTest/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users.Take(10).ToListAsync();
                var count = users.Count;
                return Ok(new { 
                    message = "Database connection successful!", 
                    userCount = count,
                    users = users.Select(u => new { 
                        id = u.Id,
                        username = u.Username,
                        email = u.Email
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
