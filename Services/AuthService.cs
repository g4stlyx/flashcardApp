using flashcardApp.Models;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DotNetEnv;

namespace flashcardApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpiryDays;
        private readonly string _pepper;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
            
            // Load settings from environment variables
            _pepper = Env.GetString("PASSWORD_PEPPER");
            _jwtSecret = Env.GetString("JWT_SECRET");
            _jwtIssuer = Env.GetString("JWT_ISSUER");
            _jwtAudience = Env.GetString("JWT_AUDIENCE");
            _jwtExpiryDays = int.Parse(Env.GetString("JWT_EXPIRE_DAYS"));
        }

        public async Task<(bool Success, string Token, string Message)> LoginAsync(string username, string password)
        {
            // Find user by username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            
            if (user == null)
            {
                return (false, string.Empty, "Yanlış kullanıcı adı veya parola");
            }

            // Verify the password with stored hash and salt
            bool isValid = VerifyPassword(password, user.PasswordHash, user.Salt);

            if (!isValid)
            {
                return (false, string.Empty, "Yanlış kullanıcı adı veya parola");
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return (true, token, "Giriş başarılı");
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password)
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            {
                return (false, "Kullanıcı Adı zaten mevcut");
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            {
                return (false, "Email zaten mevcut");
            }

            // Hash the password
            var (hash, salt) = HashPassword(password);

            // Create new user
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = hash,
                Salt = salt,
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "Kayıt başarılı");
        }

        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
                new Claim("UserType", user.IsAdmin ? "Admin" : "User")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(_jwtExpiryDays),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public bool VerifyToken(string token)
        {
            var principal = GetPrincipalFromToken(token);
            if (principal == null)
            {
                return false;
            }

            // Check if token is expired
            var expiryUnixTime = principal.Claims
                .Where(x => x.Type == JwtRegisteredClaimNames.Exp)
                .Select(x => long.Parse(x.Value))
                .FirstOrDefault();

            var expiryTime = DateTimeOffset.FromUnixTimeSeconds(expiryUnixTime).UtcDateTime;

            return expiryTime > DateTime.UtcNow;
        }

        public (string Hash, string Salt) HashPassword(string password)
        {
            // Generate a random salt
            byte[] saltBytes = new byte[32]; // 32 bytes = 256 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            // Convert salt to base64 string for storage
            var salt = Convert.ToBase64String(saltBytes);
            
            // Get the hash using Argon2id
            byte[] hashBytes = HashPasswordWithArgon2id(password, saltBytes);
            
            // Convert hash to base64 string for storage
            var hash = Convert.ToBase64String(hashBytes);
            
            return (hash, salt);
        }

        public bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] hashBytes = HashPasswordWithArgon2id(password, saltBytes);
            string newHash = Convert.ToBase64String(hashBytes);
            
            return newHash == storedHash;
        }

        private byte[] HashPasswordWithArgon2id(string password, byte[] salt)
        {
            // Combine password with pepper before hashing
            string pepperedPassword = password + _pepper;
            byte[] passwordBytes = Encoding.UTF8.GetBytes(pepperedPassword);

            // Create Argon2id instance
            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                DegreeOfParallelism = 8, // Number of threads to use
                MemorySize = 65536,      // 64MB of memory
                Iterations = 4,          // Number of iterations
                KnownSecret = null,      // Additional secret if needed (we're using pepper separately)
                AssociatedData = null    // Additional data if needed
            };

            return argon2.GetBytes(32); // Get 32 bytes (256 bits) hash
        }
    }
}
