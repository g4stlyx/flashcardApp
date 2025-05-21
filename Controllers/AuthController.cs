using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using flashcardApp.Models.Authentication;
using flashcardApp.Services;
using Microsoft.EntityFrameworkCore;
using flashcardApp.Models;

namespace flashcardApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        
        public AuthController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }
        
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var (success, token, message) = await _authService.LoginAsync(request.Username, request.Password);
            
            if (!success)
            {
                return BadRequest(new LoginResponse 
                { 
                    Success = false, 
                    Message = message 
                });
            }
            
            // get user information
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());
            
            return Ok(new LoginResponse
            {
                Success = true,
                Token = token,
                Message = "Giriş Başarılı!",
                Username = user.Username,
                IsAdmin = user.IsAdmin
            });
        }
        
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest(new RegisterResponse 
                { 
                    Success = false, 
                    Message = "Şifreler uyuşmuyor." 
                });
            }
            
            var (success, message) = await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            
            if (!success)
            {
                return BadRequest(new RegisterResponse
                {
                    Success = false,
                    Message = message
                });
            }
            
            return Ok(new RegisterResponse
            {
                Success = true,
                Message = message
            });
        }
        
        [HttpGet("validate-token")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ActionResult ValidateToken()
        {
            return Ok(new { isValid = true });
        }
        
        [HttpGet("user-info")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> GetUserInfo()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
            {
                return BadRequest("Geçersiz token!");
            }
            
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı!");
            }
            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                isAdmin = user.IsAdmin,
                firstName = user.FirstName,
                lastName = user.LastName,
                bio = user.Bio,
                profilePictureUrl = user.ProfilePictureUrl
            });
        }
        
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            // delete session state if exists
            HttpContext.Session.Clear();
            
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
