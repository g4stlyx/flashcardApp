using flashcardApp.Models;
using System.Security.Claims;

namespace flashcardApp.Services
{
    public interface IAuthService
    {
        /// Validates user credentials and returns a JWT token if valid
        Task<(bool Success, string Token, string Message)> LoginAsync(string username, string password);
        
        /// Creates a new user account with the given credentials
        Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password);
        
        /// Verifies if a JWT token is valid
        bool VerifyToken(string token);
        
        /// Generates a JWT token for a user
        string GenerateJwtToken(User user);
        
        /// Gets the claims principal from a token
        ClaimsPrincipal GetPrincipalFromToken(string token);
        
        /// Hashes a password using Argon2id with salt and pepper
        (string Hash, string Salt) HashPassword(string password);
        
        /// Verifies a password against a stored hash
        bool VerifyPassword(string password, string storedHash, string storedSalt);
    }
}
