using Microsoft.AspNetCore.Mvc;
using flashcardApp.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace flashcardApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserDiagnosticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserDiagnosticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserDiagnostics/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.IsAdmin,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/UserDiagnostics/current-user
        [HttpGet("current-user")]
        public IActionResult GetCurrentUser()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Ok(new { authenticated = false, message = "User is not authenticated" });
            }

            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                authenticated = true,
                userId,
                username,
                email,
                role,
                allClaims = claims
            });
        }

        // GET: api/UserDiagnostics/sets
        [HttpGet("sets")]
        public async Task<IActionResult> GetSets()
        {
            var sets = await _context.FlashcardSets
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.UserId,
                    OwnerUsername = s.User.Username,
                    s.CreatedAt,
                    FlashcardCount = s.Flashcards.Count
                })
                .OrderByDescending(s => s.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Ok(sets);
        }

        // GET: api/UserDiagnostics/user-sets/{userId}
        [HttpGet("user-sets/{userId}")]
        public async Task<IActionResult> GetUserSets(int userId)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, u.Username, u.Email })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var sets = await _context.FlashcardSets
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.UserId,
                    s.CreatedAt,
                    FlashcardCount = s.Flashcards.Count
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                user,
                sets
            });
        }
    }
}
