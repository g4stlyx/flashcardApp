using System.Security.Claims;
using System.Threading.Tasks;
using flashcardApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace flashcardApp.Controllers
{
    [Route("api/friends")]
    [ApiController]
    public class ApiCancelFriendRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApiCancelFriendRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/friends/cancel-request
        [HttpPost("cancel-request")]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> CancelFriendRequest([FromBody] RequestIdModel model)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == model.RequestId && r.SenderId == currentUserId);
                
            if (request == null)
            {
                return NotFound(new { message = "İstek bulunamadı." });
            }
            
            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Arkadaşlık isteği iptal edildi." });
        }

        // request model
        public class RequestIdModel
        {
            public int RequestId { get; set; }
        }
    }
}
