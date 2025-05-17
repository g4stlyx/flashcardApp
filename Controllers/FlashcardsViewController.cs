using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using flashcardApp.Models;
using flashcardApp.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace flashcardApp.Controllers
{
    public class FlashcardsViewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlashcardsViewController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /FlashcardsView
        public async Task<IActionResult> Index()
        {
            var publicSets = await _context.FlashcardSets
                .Where(s => s.Visibility == Visibility.Public)
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(publicSets);
        }          

        // GET: /FlashcardsView/MySets
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> MySets()
        {
            Console.WriteLine("MySets (GET) action called");
            
            // Log detailed request information
            Console.WriteLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
            Console.WriteLine($"Request URL: {Request.Path}{Request.QueryString}");
            Console.WriteLine($"HTTP Method: {Request.Method}");
            
            // Log headers
            Console.WriteLine("Headers:");
            foreach (var header in Request.Headers)
            {
                Console.WriteLine($"  {header.Key}: {header.Value}");
            }
            
            // Log all available auth methods
            Console.WriteLine("Auth Methods Available:");
            Console.WriteLine($"  Auth Header: {Request.Headers.ContainsKey("Authorization")}");
            Console.WriteLine($"  Query Token: {Request.Query.ContainsKey("token")}");
            Console.WriteLine($"  Cookie JWT: {Request.Cookies.ContainsKey("jwt")}");
            Console.WriteLine($"  User Principal: {User?.Identity?.IsAuthenticated}");
            
            // Make sure we have a valid user ID in the claims
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("No valid NameIdentifier claim found in User principal, trying to extract from token...");
                
                string token = null;
                
                // Check for JWT token in Authorization header (primary method)
                string authHeader = Request.Headers["Authorization"];
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                    Console.WriteLine($"Found JWT in Authorization header: {token.Substring(0, Math.Min(20, token.Length))}...");
                }
                
                // Check for token as a query parameter as a fallback
                if (string.IsNullOrEmpty(token) && Request.Query.TryGetValue("token", out var queryToken))
                {
                    token = queryToken.ToString();
                    Console.WriteLine($"Found JWT in query parameter: {token.Substring(0, Math.Min(20, token.Length))}...");
                }
                
                // Check for JWT in cookie as last resort (but we're trying to avoid relying on cookies)
                if (string.IsNullOrEmpty(token) && Request.Cookies.TryGetValue("jwt", out string jwtFromCookie))
                {
                    token = jwtFromCookie;
                    Console.WriteLine($"Found JWT cookie in GET request: {token.Substring(0, Math.Min(20, token.Length))}...");
                }
                
                if (!string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Found token, using POST method version");
                    return await MySets(token);
                }
                
                Console.WriteLine("No valid authentication found through any method, redirecting to login");
                return RedirectToAction("Login", "AuthView");
            }
            
            Console.WriteLine($"Using claim-based authentication, user ID: {userIdClaim}");
            var userId = int.Parse(userIdClaim);
            
            // Debug information about the user ID
            Console.WriteLine($"[DEBUG] User ID from token: {userId}");
            
            // Check if URL has a userId parameter that should override the token's userId
            // This is specifically added to fix the issue where token claims might be wrong
            int? urlProvidedUserId = null;
            if (Request.Query.ContainsKey("userId") && int.TryParse(Request.Query["userId"], out int parsedId))
            {
                urlProvidedUserId = parsedId;
                Console.WriteLine($"[DEBUG] URL provided user ID: {urlProvidedUserId}");
            }
            
            Console.WriteLine($"[DEBUG] All registered users in database:");
            var allUsers = await _context.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                Console.WriteLine($"  ID: {user.Id}, Username: {user.Username}, Email: {user.Email}");
            }
            
            // If URL provided a userId, verify it's a valid user ID first
            if (urlProvidedUserId.HasValue)
            {
                var userFromUrl = await _context.Users.FirstOrDefaultAsync(u => u.Id == urlProvidedUserId.Value);
                if (userFromUrl != null)
                {
                    userId = urlProvidedUserId.Value;
                    Console.WriteLine($"[DEBUG] Using URL-provided user ID: {userId} (Username: {userFromUrl.Username})");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] URL-provided user ID {urlProvidedUserId} not found in database, using token ID");
                }
            }
            
            Console.WriteLine($"[DEBUG] Looking for sets with UserId = {userId}");
            
            // Maybe there was an error in lookup or database seed: verify user exists
            var tokenUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (tokenUser == null)
            {
                Console.WriteLine($"[ERROR] User with ID {userId} from token not found in database!");
                
                // Try to find a user by the username claim instead
                var usernameClaim = User.FindFirstValue(ClaimTypes.Name);
                if (!string.IsNullOrEmpty(usernameClaim))
                {
                    Console.WriteLine($"[DEBUG] Trying to find user by username: {usernameClaim}");
                    tokenUser = await _context.Users.FirstOrDefaultAsync(u => 
                        u.Username.ToLower() == usernameClaim.ToLower());
                        
                    if (tokenUser != null)
                    {
                        Console.WriteLine($"[DEBUG] Found user by username: ID={tokenUser.Id}");
                        userId = tokenUser.Id;
                    }
                }
            }
            
            try
            {
                // First try getting sets without flashcards to be safe
                var mySets = await _context.FlashcardSets
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
                    
                // Then manually load flashcards to avoid potential mapping issues
                foreach (var set in mySets)
                {
                    // Manually load flashcards for each set
                    var flashcards = await _context.Flashcards
                        .Where(f => f.SetId == set.Id)
                        .ToListAsync();
                        
                    // Assign to the navigation property
                    set.Flashcards = flashcards;
                }
                
                return View(mySets);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error retrieving flashcard sets: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Return a simple view without flashcards as fallback
                var simpleSets = await _context.FlashcardSets
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new FlashcardSet
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Description = s.Description,
                        UserId = s.UserId,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                        Visibility = s.Visibility
                    })
                    .ToListAsync();
                    
                return View(simpleSets);
            }
        }
        
        // POST: /FlashcardsView/MySets - Alternate approach for form submit with JWT
        [HttpPost]
        public async Task<IActionResult> MySets(string jwt)
        {
            Console.WriteLine("MySets (POST) action called");
            
            // Process the JWT token
            if (string.IsNullOrEmpty(jwt))
            {
                Console.WriteLine("No JWT provided in POST");
                return RedirectToAction("Login", "AuthView");
            }
            
            Console.WriteLine($"Received JWT token: {jwt.Substring(0, Math.Min(20, jwt.Length))}...");
            
            // Add the token to the Authorization header for future requests
            HttpContext.Response.Headers.Append("X-JWT-Status", "Valid");
            
            // Let's try adding a user ID cookie to ensure consistent user identification
            var jwtHandler = new JwtSecurityTokenHandler();
            var parsedToken = jwtHandler.ReadToken(jwt) as JwtSecurityToken;
            var userIdFromClaims = parsedToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userIdFromClaims))
            {
                Console.WriteLine($"Setting user_id cookie to {userIdFromClaims}");
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = false,  // Allow JavaScript to access
                    Secure = HttpContext.Request.IsHttps,  // Secure in production
                    SameSite = SameSiteMode.Lax,  // Allow some cross-site requests
                    Expires = DateTime.UtcNow.AddDays(7)  // 7 days expiry
                };
                
                Response.Cookies.Append("user_id", userIdFromClaims, cookieOptions);
            }
            
            try
            {
                // Parse the token claims
                var tokenClaims = parsedToken?.Claims;
                if (tokenClaims == null)
                {
                    Console.WriteLine("Invalid token: no claims found");
                    return RedirectToAction("Login", "AuthView");
                }
                
                // Log all claims for debugging
                Console.WriteLine("Claims in token:");
                foreach (var claim in tokenClaims)
                {
                    Console.WriteLine($"  {claim.Type}: {claim.Value}");
                }
                
                var userIdClaim = tokenClaims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    Console.WriteLine("Invalid token: missing NameIdentifier claim");
                    return RedirectToAction("Login", "AuthView");
                }
                
                int userIdToUse = int.Parse(userIdClaim.Value);
                Console.WriteLine($"Using user ID from token: {userIdToUse}");
                
                // First try getting sets without flashcards to be safe
                var mySets = await _context.FlashcardSets
                    .Where(s => s.UserId == userIdToUse)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
                    
                // Then manually load flashcards to avoid potential mapping issues
                foreach (var set in mySets)
                {
                    // Manually load flashcards for each set
                    var flashcards = await _context.Flashcards
                        .Where(f => f.SetId == set.Id)
                        .ToListAsync();
                        
                    // Assign to the navigation property
                    set.Flashcards = flashcards;
                }
                
                return View(mySets);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error retrieving flashcard sets: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Get user ID from token
                int userIdForError = 0;
                if (!string.IsNullOrEmpty(userIdFromClaims) && int.TryParse(userIdFromClaims, out int parsedId))
                {
                    userIdForError = parsedId;
                }
                
                // Return a simple view without flashcards as fallback
                var simpleSets = await _context.FlashcardSets
                    .Where(s => s.UserId == userIdForError)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new FlashcardSet
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Description = s.Description,
                        UserId = s.UserId,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                        Visibility = s.Visibility
                    })
                    .ToListAsync();
                    
                return View(simpleSets);
            }
        }

        // GET: /FlashcardsView/Set/5
        public async Task<IActionResult> Set(int id)
        {
            // Get the set and include flashcards
            var set = await _context.FlashcardSets
                .Include(s => s.User)
                .Include(s => s.Flashcards)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (set == null)
            {
                return NotFound();
            }
            
            // Check visibility permissions
            if (set.Visibility == Visibility.Private)
            {
                // For private sets, check if user is authenticated and is the owner
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "AuthView");
                }
                
                var userIdFromClaim = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (set.UserId != userIdFromClaim)
                {
                    return Forbid();
                }
            }
            
            // Increment view count
            try
            {
                // Create a new SetView record
                _context.SetViews.Add(new SetView 
                { 
                    SetId = set.Id,
                    ViewedAt = DateTime.UtcNow
                });
                
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Fail silently - view counting is not critical
            }
            
            return View(set);
        }
        
        // POST handling for Set action with JWT token
        [HttpPost]
        public async Task<IActionResult> Set(int id, string jwt)
        {
            // Process the JWT token
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "AuthView");
            }
            
            // Set the JWT token in the response cookies
            HttpContext.Response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            
            // Continue with the regular action
            var set = await _context.FlashcardSets
                .Include(s => s.User)
                .Include(s => s.Flashcards)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (set == null)
            {
                return NotFound();
            }
            
            // Check visibility permissions for private sets
            if (set.Visibility == Visibility.Private)
            {
                // Validate the JWT token manually
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonTokenObj = tokenHandler.ReadToken(jwt) as JwtSecurityToken;
                
                if (jsonTokenObj == null)
                {
                    return RedirectToAction("Login", "AuthView");
                }
                
                var userIdClaim = jsonTokenObj.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return RedirectToAction("Login", "AuthView");
                }
                
                var userIdFromToken = int.Parse(userIdClaim.Value);
                if (set.UserId != userIdFromToken)
                {
                    return Forbid();
                }
            }
            
            // Increment view count
            try
            {
                _context.SetViews.Add(new SetView 
                { 
                    SetId = set.Id,
                    ViewedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Fail silently - view counting is not critical
            }
            
            return View(set);
        }
        
        // GET: /FlashcardsView/Study/5
        public async Task<IActionResult> Study(int id)
        {
            var set = await _context.FlashcardSets
                .Include(s => s.Flashcards)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (set == null)
            {
                return NotFound();
            }
            
            // Check visibility permissions
            if (set.Visibility == Visibility.Private)
            {
                // For private sets, check if user is authenticated and is the owner
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "AuthView");
                }
                
                var userIdFromClaim = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (set.UserId != userIdFromClaim)
                {
                    return Forbid();
                }
            }
            
            return View(set);
        }
        
        // POST handling for Study action with JWT token
        [HttpPost]
        public async Task<IActionResult> Study(int id, string jwt)
        {
            // Process the JWT token
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "AuthView");
            }
            
            // Set the JWT token in the response cookies
            HttpContext.Response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            
            // Continue with the regular action
            var set = await _context.FlashcardSets
                .Include(s => s.Flashcards)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (set == null)
            {
                return NotFound();
            }
            
            // Check visibility permissions for private sets
            if (set.Visibility == Visibility.Private)
            {
                // Validate the JWT token manually
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonTokenObj = tokenHandler.ReadToken(jwt) as JwtSecurityToken;
                
                if (jsonTokenObj == null)
                {
                    return RedirectToAction("Login", "AuthView");
                }
                
                var userIdClaim = jsonTokenObj.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return RedirectToAction("Login", "AuthView");
                }
                
                var userIdFromToken = int.Parse(userIdClaim.Value);
                if (set.UserId != userIdFromToken)
                {
                    return Forbid();
                }
            }
            
            return View(set);
        }

        // GET: /FlashcardsView/Create
        [Authentication.JwtAuthorize("Registered")]
        public IActionResult Create()
        {
            return View();
        }
        
        // POST: /FlashcardsView/Create (with JWT form handling)
        [HttpPost]
        public IActionResult Create(string jwt)
        {
            // Process the JWT token
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "AuthView");
            }
            
            // Set the JWT token in the response cookies
            HttpContext.Response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            
            // Continue to the Create view
            return View();
        }

        // GET: /FlashcardsView/Edit/5
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> Edit(int id, string token = null)
        {
            Console.WriteLine("Edit (GET) action called for id " + id);
            Console.WriteLine("Token provided as query parameter: " + (token != null ? "Yes (length: " + token.Length + ")" : "No"));
            
            // Log authorization status
            Console.WriteLine("Request Authorization Header: " + (Request.Headers.ContainsKey("Authorization") ? Request.Headers["Authorization"].ToString() : "None"));
            Console.WriteLine("User authenticated: " + (User.Identity?.IsAuthenticated == true ? "Yes" : "No"));
            
            // Try to verify the user through various methods (token in query, Authorization header, or already authenticated)
            int userId;
            
            // First check if user is already authenticated through standard mechanisms
            if (User.Identity?.IsAuthenticated == true) 
            {
                Console.WriteLine("User is already authenticated via standard mechanisms");
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                Console.WriteLine("Using authenticated user ID: " + userId);
            }
            // Then try using the token provided as parameter
            else if (!string.IsNullOrEmpty(token)) 
            {
                Console.WriteLine("Using token provided as parameter");
                var handler = new JwtSecurityTokenHandler();
                try {
                    var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                    var userIdClaim = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    if (userIdClaim == null) {
                        Console.WriteLine("No valid user ID found in token");
                        return RedirectToAction("Login", "AuthView");
                    }
                    userId = int.Parse(userIdClaim.Value);
                    Console.WriteLine("Using user ID from query token: " + userId);
                } catch (Exception ex) {
                    Console.WriteLine("Error processing token: " + ex.Message);
                    return RedirectToAction("Login", "AuthView");
                }
            }
            // Finally try using Authorization header
            else if (Request.Headers.ContainsKey("Authorization"))
            {
                Console.WriteLine("Using Authorization header");
                string authHeader = Request.Headers["Authorization"];
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ")) {
                    Console.WriteLine("Invalid Authorization header format");
                    return RedirectToAction("Login", "AuthView");
                }
                
                string headerToken = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                try {
                    var jsonToken = handler.ReadToken(headerToken) as JwtSecurityToken;
                    var userIdClaim = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    if (userIdClaim == null) {
                        Console.WriteLine("No valid user ID found in header token");
                        return RedirectToAction("Login", "AuthView");
                    }
                    userId = int.Parse(userIdClaim.Value);
                    Console.WriteLine("Using user ID from header token: " + userId);
                } catch (Exception ex) {
                    Console.WriteLine("Error processing header token: " + ex.Message);
                    return RedirectToAction("Login", "AuthView");
                }
            }
            else 
            {
                Console.WriteLine("No authentication provided, redirecting to login");
                return RedirectToAction("Login", "AuthView");
            }
            
            var set = await _context.FlashcardSets
                .Include(s => s.Flashcards)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (set == null)
            {
                return NotFound();
            }
            
            // Check if user is the owner
            if (set.UserId != userId)
            {
                return Forbid();
            }
            
            return View(set);
        }
        
        // POST: /FlashcardsView/Edit/5 (with JWT form handling)
        [HttpPost]
        public async Task<IActionResult> EditPost(int id, string jwt)
        {
            // Process the JWT token
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "AuthView");
            }
            
            // Set the JWT token in the response cookies
            HttpContext.Response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            
            // Manually validate the token to check ownership
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwt) as JwtSecurityToken;
            
            if (jsonToken == null)
            {
                return RedirectToAction("Login", "AuthView");
            }
            
            var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return RedirectToAction("Login", "AuthView");
            }
            
            var userId = int.Parse(userIdClaim.Value);
            
            // Check if the set exists and the user is the owner
            var set = await _context.FlashcardSets
                .Include(s => s.Flashcards)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (set == null)
            {
                return NotFound();
            }
            
            if (set.UserId != userId)
            {
                return Forbid();
            }
            
            return View(set);
        }
        
        // GET: /FlashcardsView/Friends
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> Friends()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Get all pending friend requests received by the user
            var receivedRequests = await _context.FriendRequests
                .Where(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending)
                .Include(r => r.Sender)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
                
            // Get all friend requests sent by the user
            var sentRequests = await _context.FriendRequests
                .Where(r => r.SenderId == userId)
                .Include(r => r.Receiver)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
                
            // Get all friends of the user
            var friends = new List<User>();
            
            // Get friends where the user is UserId1
            var friendships1 = await _context.Friends
                .Where(f => f.UserId1 == userId)
                .Include(f => f.User2)
                .ToListAsync();
                
            // Get friends where the user is UserId2
            var friendships2 = await _context.Friends
                .Where(f => f.UserId2 == userId)
                .Include(f => f.User1)
                .ToListAsync();
                
            // Add User2 objects from friendships1
            friends.AddRange(friendships1.Select(f => f.User2));
            
            // Add User1 objects from friendships2
            friends.AddRange(friendships2.Select(f => f.User1));
            
            // Create the view model
            var model = new 
            {
                ReceivedRequests = receivedRequests,
                SentRequests = sentRequests,
                Friends = friends
            };
            
            return View(model);
        }
        
        // POST: /FlashcardsView/SendFriendRequest
        [HttpPost]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> SendFriendRequest(string username)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Find the target user
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (targetUser == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }
            
            // Don't allow sending request to self
            if (targetUser.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Kendinize arkadaşlık isteği gönderemezsiniz.";
                return RedirectToAction(nameof(Friends));
            }
            
            // Check if already friends
            var existingFriendship = await _context.Friends
                .AnyAsync(f => (f.UserId1 == currentUserId && f.UserId2 == targetUser.Id) || 
                              (f.UserId1 == targetUser.Id && f.UserId2 == currentUserId));
                              
            if (existingFriendship)
            {
                TempData["ErrorMessage"] = "Bu kullanıcı zaten arkadaşınız.";
                return RedirectToAction(nameof(Friends));
            }
            
            // Check if a request already exists
            var existingRequest = await _context.FriendRequests
                .FirstOrDefaultAsync(r => 
                    (r.SenderId == currentUserId && r.ReceiverId == targetUser.Id) ||
                    (r.SenderId == targetUser.Id && r.ReceiverId == currentUserId));
                    
            if (existingRequest != null)
            {
                if (existingRequest.SenderId == currentUserId)
                {
                    TempData["ErrorMessage"] = "Bu kullanıcıya zaten bir istek gönderdiniz.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Bu kullanıcı size zaten bir istek göndermiş. Arkadaşlık isteği sayfanızı kontrol edin.";
                }
                return RedirectToAction(nameof(Friends));
            }
            
            // Create the friend request
            var friendRequest = new FriendRequest
            {
                SenderId = currentUserId,
                ReceiverId = targetUser.Id,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.FriendRequests.Add(friendRequest);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Arkadaşlık isteği gönderildi.";
            return RedirectToAction(nameof(Friends));
        }
        
        // POST: /FlashcardsView/AcceptFriendRequest
        [HttpPost]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> AcceptFriendRequest(int requestId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Find the request
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == currentUserId);
                
            if (request == null)
            {
                TempData["ErrorMessage"] = "İstek bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }
            
            // Update request status
            request.Status = FriendRequestStatus.Accepted;
            
            // Create friendship
            var friendship = new Friend
            {
                UserId1 = request.SenderId,
                UserId2 = request.ReceiverId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Friends.Add(friendship);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Arkadaşlık isteği kabul edildi.";
            return RedirectToAction(nameof(Friends));
        }
        
        // POST: /FlashcardsView/DeclineFriendRequest
        [HttpPost]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> DeclineFriendRequest(int requestId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Find the request
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == currentUserId);
                
            if (request == null)
            {
                TempData["ErrorMessage"] = "İstek bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }
            
            // Remove the request instead of just updating its status
            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Arkadaşlık isteği reddedildi.";
            return RedirectToAction(nameof(Friends));
        }
        
        // POST: /FlashcardsView/CancelFriendRequest
        [HttpPost]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> CancelFriendRequest(int requestId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Find the request
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.SenderId == currentUserId);
                
            if (request == null)
            {
                TempData["ErrorMessage"] = "İstek bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }
            
            // Remove the request
            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Arkadaşlık isteği iptal edildi.";
            return RedirectToAction(nameof(Friends));
        }
        
        // POST: /FlashcardsView/RemoveFriend
        [HttpPost]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Find the friendship
            var friendship = await _context.Friends
                .FirstOrDefaultAsync(f => 
                    (f.UserId1 == currentUserId && f.UserId2 == friendId) || 
                    (f.UserId1 == friendId && f.UserId2 == currentUserId));
                    
            if (friendship == null)
            {
                TempData["ErrorMessage"] = "Arkadaşlık bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }
            
            // Remove the friendship
            _context.Friends.Remove(friendship);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Arkadaşlık silindi.";
            return RedirectToAction(nameof(Friends));
        }
        
        // GET: /FlashcardsView/UserSets/{id}
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> UserSets(int id)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Verify these users are friends
            var areFriends = await _context.Friends
                .AnyAsync(f => 
                    (f.UserId1 == currentUserId && f.UserId2 == id) || 
                    (f.UserId1 == id && f.UserId2 == currentUserId));
                    
            if (!areFriends && currentUserId != id)
            {
                TempData["ErrorMessage"] = "Bu kullanıcının setlerini görüntüleme izniniz yok.";
                return RedirectToAction(nameof(Friends));
            }
            
            // Get the user's sets - include friends-only sets if they are friends
            var sets = await _context.FlashcardSets
                .Where(s => s.UserId == id && 
                           (s.Visibility == Visibility.Public || 
                            (s.Visibility == Visibility.Friends && areFriends) || 
                            currentUserId == id))
                .Include(s => s.User)
                .Include(s => s.Flashcards)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
                
            ViewBag.Username = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();
                
            return View(sets);
        }
        
        // GET: /FlashcardsView/FriendSets
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> FriendSets()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Get all friends of the user
            var friendIds = new List<int>();
            
            // Get friends where the user is UserId1
            var friendships1 = await _context.Friends
                .Where(f => f.UserId1 == currentUserId)
                .Select(f => f.UserId2)
                .ToListAsync();
                
            // Get friends where the user is UserId2
            var friendships2 = await _context.Friends
                .Where(f => f.UserId2 == currentUserId)
                .Select(f => f.UserId1)
                .ToListAsync();
                
            // Combine all friend IDs
            friendIds.AddRange(friendships1);
            friendIds.AddRange(friendships2);
            
            // Get both public and friends-only flashcard sets
            var friendSets = await _context.FlashcardSets
                .Where(s => friendIds.Contains(s.UserId) && 
                           (s.Visibility == Visibility.Public || s.Visibility == Visibility.Friends))
                .Include(s => s.User)
                .Include(s => s.Flashcards)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
                
            return View(friendSets);
        }

        // GET: api/friends/pending-requests
        [HttpGet("api/friends/pending-requests")]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> GetPendingFriendRequests()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Get count of pending friend requests received by the current user
            var pendingRequestsCount = await _context.FriendRequests
                .CountAsync(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending);
                
            return Json(new { pendingCount = pendingRequestsCount });
        }
        
        // POST: api/friends/send-request
        [HttpPost("api/friends/send-request")]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> ApiSendFriendRequest([FromBody] ApiRequestModel model)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Find the target user
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (targetUser == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }
            
            // Don't allow sending request to self
            if (targetUser.Id == currentUserId)
            {
                return BadRequest(new { message = "Kendinize arkadaşlık isteği gönderemezsiniz." });
            }
            
            // Check if already friends
            var existingFriendship = await _context.Friends
                .AnyAsync(f => (f.UserId1 == currentUserId && f.UserId2 == targetUser.Id) || 
                              (f.UserId1 == targetUser.Id && f.UserId2 == currentUserId));
                              
            if (existingFriendship)
            {
                return BadRequest(new { message = "Bu kullanıcı zaten arkadaşınız." });
            }
            
            // Check if a request already exists
            var existingRequest = await _context.FriendRequests
                .FirstOrDefaultAsync(r => 
                    (r.SenderId == currentUserId && r.ReceiverId == targetUser.Id) ||
                    (r.SenderId == targetUser.Id && r.ReceiverId == currentUserId));
                    
            if (existingRequest != null)
            {
                if (existingRequest.SenderId == currentUserId)
                {
                    return BadRequest(new { message = "Bu kullanıcıya zaten bir istek gönderdiniz." });
                }
                else
                {
                    return BadRequest(new { message = "Bu kullanıcı size zaten bir istek göndermiş." });
                }
            }
            
            // Create the friend request
            var friendRequest = new FriendRequest
            {
                SenderId = currentUserId,
                ReceiverId = targetUser.Id,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.FriendRequests.Add(friendRequest);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Arkadaşlık isteği gönderildi." });
        }
        
        // POST: api/friends/accept-request
        [HttpPost("api/friends/accept-request")]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> ApiAcceptFriendRequest([FromBody] ApiRequestIdModel model)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Find the request
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == model.RequestId && r.ReceiverId == currentUserId);
                
            if (request == null)
            {
                return NotFound(new { message = "İstek bulunamadı." });
            }
            
            // Update request status
            request.Status = FriendRequestStatus.Accepted;
            
            // Create friendship
            var friendship = new Friend
            {
                UserId1 = request.SenderId,
                UserId2 = request.ReceiverId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Friends.Add(friendship);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Arkadaşlık isteği kabul edildi." });
        }
        
        // POST: api/friends/decline-request
        [HttpPost("api/friends/decline-request")]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> ApiDeclineFriendRequest([FromBody] ApiRequestIdModel model)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Find the request
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == model.RequestId && r.ReceiverId == currentUserId);
                
            if (request == null)
            {
                return NotFound(new { message = "İstek bulunamadı." });
            }
            
            // Remove the request instead of just updating its status
            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Arkadaşlık isteği reddedildi." });
        }
        
        // Models for API requests
        public class ApiRequestModel
        {
            public string Username { get; set; }
        }
        
        public class ApiRequestIdModel
        {
            public int RequestId { get; set; }
        }
    }
}
