using flashcardApp.Models;
using System.Security.Claims;

namespace flashcardApp.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Token, string Message)> LoginAsync(string username, string password);
        Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password);        
        bool VerifyToken(string token);        
        string GenerateJwtToken(User user);        
        ClaimsPrincipal GetPrincipalFromToken(string token);        
        (string Hash, string Salt) HashPassword(string password);        
        bool VerifyPassword(string password, string storedHash, string storedSalt);
    }
}
