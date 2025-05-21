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
        public async Task<IActionResult> MySets(string? token = null)
        {
            Console.WriteLine("MySets (GET) action called");

            Console.WriteLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
            Console.WriteLine($"Request URL: {Request.Path}{Request.QueryString}");
            Console.WriteLine($"HTTP Method: {Request.Method}");
            Console.WriteLine($"Token parameter provided: {!string.IsNullOrEmpty(token)}");

            Console.WriteLine("Headers:");
            foreach (var header in Request.Headers)
            {
                Console.WriteLine($"  {header.Key}: {header.Value}");
            }

            Console.WriteLine("Auth Methods Available:");
            Console.WriteLine($"  Auth Header: {Request.Headers.ContainsKey("Authorization")}");
            Console.WriteLine($"  Query Token: {Request.Query.ContainsKey("token")}");
            Console.WriteLine($"  Cookie JWT: {Request.Cookies.ContainsKey("jwt")}");
            Console.WriteLine($"  User Principal: {User?.Identity?.IsAuthenticated}");

            // put token from the query to headers
            if (!string.IsNullOrEmpty(token) && !Request.Headers.ContainsKey("Authorization"))
            {
                Console.WriteLine("Using token from query parameter for authentication");
                HttpContext.Items["ManualToken"] = token;
            }

            // is user valid?
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("No valid NameIdentifier claim found in User principal, trying to extract from token...");

                string extractedToken = null;

                // check for JWT in auth header
                string authHeader = Request.Headers["Authorization"];
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    extractedToken = authHeader.Substring("Bearer ".Length).Trim();
                    Console.WriteLine($"Found JWT in Authorization header: {extractedToken.Substring(0, Math.Min(20, extractedToken.Length))}...");
                }

                // check for token as a query parameter
                if (string.IsNullOrEmpty(extractedToken) && Request.Query.TryGetValue("token", out var queryToken))
                {
                    extractedToken = queryToken.ToString();
                    Console.WriteLine($"Found JWT in query parameter: {extractedToken.Substring(0, Math.Min(20, extractedToken.Length))}...");
                }

                // check for JWT in cookies as last solution
                if (string.IsNullOrEmpty(extractedToken) && Request.Cookies.TryGetValue("jwt", out string jwtFromCookie))
                {
                    extractedToken = jwtFromCookie;
                    Console.WriteLine($"Found JWT cookie in GET request: {extractedToken.Substring(0, Math.Min(20, extractedToken.Length))}...");
                }

                if (!string.IsNullOrEmpty(extractedToken))
                {
                    Console.WriteLine("Found token, using POST method version");
                    return await MySetsPost(extractedToken);
                }

                Console.WriteLine("No valid authentication found through any method, redirecting to login");
                return RedirectToAction("Login", "AuthView");
            }

            Console.WriteLine($"Using claim-based authentication, user ID: {userIdClaim}");
            var userId = int.Parse(userIdClaim);

            Console.WriteLine($"[DEBUG] User ID from token: {userId}");

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

            // verify the input id as a valid user ID first
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

            // verify user exists
            var tokenUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (tokenUser == null)
            {
                Console.WriteLine($"[ERROR] User with ID {userId} from token not found in database!");

                // try to find a user by the username claim instead
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
                // first try getting sets without flashcards
                var mySets = await _context.FlashcardSets
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                // then load flashcards to avoid potential mapping issues
                foreach (var set in mySets)
                {
                    // manually load flashcards for each set
                    var flashcards = await _context.Flashcards
                        .Where(f => f.SetId == set.Id)
                        .ToListAsync();

                    set.Flashcards = flashcards;
                }

                return View(mySets);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving flashcard sets: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // return a simple view without flashcards
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

        // POST: /FlashcardsView/MySets
        [HttpPost]
        [Route("FlashcardsView/MySets")]
        public async Task<IActionResult> MySetsPost(string jwt)
        {
            Console.WriteLine("MySets (POST) action called");

            // check the JWT
            if (string.IsNullOrEmpty(jwt))
            {
                Console.WriteLine("No JWT provided in POST");
                return RedirectToAction("Login", "AuthView");
            }

            Console.WriteLine($"Received JWT token: {jwt.Substring(0, Math.Min(20, jwt.Length))}...");

            // add token to auth header
            HttpContext.Response.Headers.Append("X-JWT-Status", "Valid");

            // add a user ID cookie
            var jwtHandler = new JwtSecurityTokenHandler();
            var parsedToken = jwtHandler.ReadToken(jwt) as JwtSecurityToken;
            var userIdFromClaims = parsedToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdFromClaims))
            {
                Console.WriteLine($"Setting user_id cookie to {userIdFromClaims}");
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = false,  // allow JS to access
                    Secure = HttpContext.Request.IsHttps,  // secure in HTTPS (prod)
                    SameSite = SameSiteMode.Lax,  // allow some cross-site requests
                    Expires = DateTime.UtcNow.AddDays(7)  // expires after 7 days
                };

                Response.Cookies.Append("user_id", userIdFromClaims, cookieOptions);
            }

            try
            {
                // the token claims
                var tokenClaims = parsedToken?.Claims;
                if (tokenClaims == null)
                {
                    Console.WriteLine("Invalid token: no claims found");
                    return RedirectToAction("Login", "AuthView");
                }

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

                // first get sets without flashcards
                var mySets = await _context.FlashcardSets
                    .Where(s => s.UserId == userIdToUse)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                // then manually load flashcards
                foreach (var set in mySets)
                {
                    var flashcards = await _context.Flashcards
                        .Where(f => f.SetId == set.Id)
                        .ToListAsync();

                    set.Flashcards = flashcards;
                }

                return View(mySets);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving flashcard sets: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Get user_id
                int userIdForError = 0;
                if (!string.IsNullOrEmpty(userIdFromClaims) && int.TryParse(userIdFromClaims, out int parsedId))
                {
                    userIdForError = parsedId;
                }

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
            var set = await _context.FlashcardSets
                .Include(s => s.User)
                .Include(s => s.Flashcards)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (set == null)
            {
                return NotFound();
            }

            // check visibility
            if (set.Visibility == Visibility.Private)
            {
                // if private, only owners and admins allowed
                if (!User.Identity.IsAuthenticated)
                {
                    if (Request.Query.TryGetValue("token", out var token))
                    {
                        Console.WriteLine("Found token in query for private set, trying to use it");
                        return await Set(id, token.ToString());
                    }
                    return RedirectToAction("Login", "AuthView");
                }

                var userIdFromClaim = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // check admin
                bool isAdmin = User.HasClaim(c => c.Type == "UserType" && c.Value == "Admin") || 
                              User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Admin") ||
                              User.IsInRole("Admin");
                
                Console.WriteLine($"Private set access: UserId={userIdFromClaim}, SetOwner={set.UserId}, IsAdmin={isAdmin}");

                if (set.UserId != userIdFromClaim && !isAdmin)
                {
                    return Forbid();
                }
            }

            // view count++
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
            }

            return View(set);
        }

        // POST: Set action with JWT
        [HttpPost]
        public async Task<IActionResult> Set(int id, string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "AuthView");
            }

            HttpContext.Response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            var set = await _context.FlashcardSets
                .Include(s => s.User)
                .Include(s => s.Flashcards)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (set == null)
            {
                return NotFound();
            }

            if (set.Visibility == Visibility.Private)
            {
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

                var userTypeClaim = jsonTokenObj.Claims.FirstOrDefault(c => c.Type == "UserType");
                var roleClaim = jsonTokenObj.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                
                bool isAdmin = (userTypeClaim != null && userTypeClaim.Value == "Admin") ||
                               (roleClaim != null && roleClaim.Value == "Admin");

                if (set.UserId != userIdFromToken && !isAdmin)
                {
                    return Forbid();
                }
            }

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

            if (set.Visibility == Visibility.Private)
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "AuthView");
                }

                var userIdFromClaim = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isAdmin = User.HasClaim(c => c.Type == "UserType" && c.Value == "Admin");

                if (set.UserId != userIdFromClaim && !isAdmin)
                {
                    return Forbid();
                }
            }

            return View(set);
        }

        // POST: study action with JWT
        [HttpPost]
        public async Task<IActionResult> Study(int id, string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "AuthView");
            }

            HttpContext.Response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            var set = await _context.FlashcardSets
                .Include(s => s.Flashcards)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (set == null)
            {
                return NotFound();
            }

            if (set.Visibility == Visibility.Private)
            {
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

                var userTypeClaim = jsonTokenObj.Claims.FirstOrDefault(c => c.Type == "UserType");
                bool isAdmin = userTypeClaim != null && userTypeClaim.Value == "Admin";

                if (set.UserId != userIdFromToken && !isAdmin)
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

        // POST: /FlashcardsView/Create (+ JWT)
        [HttpPost]
        public IActionResult Create(string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "AuthView");
            }

            HttpContext.Response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return View();
        }

        // GET: /FlashcardsView/Edit/5
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> Edit(int id, string? token = null)
        {
            Console.WriteLine("Edit (GET) action called for id " + id);
            Console.WriteLine("Token provided as query parameter: " + (token != null ? "Yes (length: " + token.Length + ")" : "No"));

            Console.WriteLine("Request Authorization Header: " + (Request.Headers.ContainsKey("Authorization") ? Request.Headers["Authorization"].ToString() : "None"));
            Console.WriteLine("User authenticated: " + (User.Identity?.IsAuthenticated == true ? "Yes" : "No"));

            int userId;

            if (User.Identity?.IsAuthenticated == true)
            {
                Console.WriteLine("User is already authenticated via standard mechanisms");
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                Console.WriteLine("Using authenticated user ID: " + userId);
            }
            else if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Using token provided as parameter");
                var handler = new JwtSecurityTokenHandler();
                try
                {
                    var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                    var userIdClaim = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    if (userIdClaim == null)
                    {
                        Console.WriteLine("No valid user ID found in token");
                        return RedirectToAction("Login", "AuthView");
                    }
                    userId = int.Parse(userIdClaim.Value);
                    Console.WriteLine("Using user ID from query token: " + userId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing token: " + ex.Message);
                    return RedirectToAction("Login", "AuthView");
                }
            }
            else if (Request.Headers.ContainsKey("Authorization"))
            {
                Console.WriteLine("Using Authorization header");
                string authHeader = Request.Headers["Authorization"];
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    Console.WriteLine("Invalid Authorization header format");
                    return RedirectToAction("Login", "AuthView");
                }

                string headerToken = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                try
                {
                    var jsonToken = handler.ReadToken(headerToken) as JwtSecurityToken;
                    var userIdClaim = jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    if (userIdClaim == null)
                    {
                        Console.WriteLine("No valid user ID found in header token");
                        return RedirectToAction("Login", "AuthView");
                    }
                    userId = int.Parse(userIdClaim.Value);
                    Console.WriteLine("Using user ID from header token: " + userId);
                }
                catch (Exception ex)
                {
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

            bool isAdmin = User.HasClaim(c => c.Type == "UserType" && c.Value == "Admin") || 
                          User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Admin") ||
                          User.IsInRole("Admin");

            Console.WriteLine($"Edit authorization check: UserId={userId}, SetOwner={set.UserId}, IsAdmin={isAdmin}");
            
            if (set.UserId != userId && !isAdmin)
            {
                Console.WriteLine("Access denied: User is not the owner or admin");
                return Forbid();
            }

            Console.WriteLine("Access granted to edit set");
            return View(set);
        }

        // POST: /FlashcardsView/Edit/5 (+ JWT)
        [HttpPost]
        public async Task<IActionResult> EditPost(int id, string jwt)
        {
            // Process the JWT token
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToAction("Login", "AuthView");
            }

            HttpContext.Response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

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

            var set = await _context.FlashcardSets
                .Include(s => s.Flashcards)
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (set == null)
            {
                return NotFound();
            }

            var userTypeClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "UserType");
            var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            
            bool isAdmin = (userTypeClaim != null && userTypeClaim.Value == "Admin") ||
                          (roleClaim != null && roleClaim.Value == "Admin");
                          
            Console.WriteLine($"EditPost authorization check: UserId={userId}, SetOwner={set.UserId}, IsAdmin={isAdmin}");
            
            if (set.UserId != userId && !isAdmin)
            {
                Console.WriteLine("Access denied: User is not the owner or admin");
                return Forbid();
            }

            Console.WriteLine("Access granted to edit set");
            return View(set);
        }

        // GET: /FlashcardsView/Friends
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> Friends(string? token = null)
        {
            Console.WriteLine("Friends (GET) action called");

            Console.WriteLine($"Token param provided: {!string.IsNullOrEmpty(token)}");
            Console.WriteLine($"Auth header present: {Request.Headers.ContainsKey("Authorization")}");

            if (!string.IsNullOrEmpty(token) && !Request.Headers.ContainsKey("Authorization"))
            {
                Console.WriteLine("Using token from query parameter for authentication in Friends view");
                HttpContext.Items["ManualToken"] = token;
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("No valid NameIdentifier claim found in User principal for Friends, trying to extract from token...");

                string extractedToken = null;

                string authHeader = Request.Headers["Authorization"];
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    extractedToken = authHeader.Substring("Bearer ".Length).Trim();
                    Console.WriteLine($"Found JWT in Authorization header: {extractedToken.Substring(0, Math.Min(20, extractedToken.Length))}...");
                }

                if (string.IsNullOrEmpty(extractedToken) && Request.Query.TryGetValue("token", out var queryToken))
                {
                    extractedToken = queryToken.ToString();
                    Console.WriteLine($"Found JWT in query parameter: {extractedToken.Substring(0, Math.Min(20, extractedToken.Length))}...");
                }

                if (string.IsNullOrEmpty(extractedToken) && Request.Cookies.TryGetValue("jwt", out string jwtFromCookie))
                {
                    extractedToken = jwtFromCookie;
                    Console.WriteLine($"Found JWT cookie in GET request: {extractedToken.Substring(0, Math.Min(20, extractedToken.Length))}...");
                }

                if (!string.IsNullOrEmpty(extractedToken))
                {
                    Console.WriteLine("Found token, using POST method version");
                    return await FriendsPost(extractedToken);
                }

                Console.WriteLine("No valid authentication found through any method, redirecting to login");
                return RedirectToAction("Login", "AuthView");
            }

            Console.WriteLine($"Using claim-based authentication for Friends, user ID: {userIdClaim}");
            var userId = int.Parse(userIdClaim);

            // get all waiting friend requests received by the user
            var receivedRequests = await _context.FriendRequests
                .Where(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending)
                .Include(r => r.Sender)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // get all friend requests sent by the user
            var sentRequests = await _context.FriendRequests
                .Where(r => r.SenderId == userId)
                .Include(r => r.Receiver)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // get all friends of the user
            var friends = new List<User>();

            // get friends where the user is UserId1
            var friendships1 = await _context.Friends
                .Where(f => f.UserId1 == userId)
                .Include(f => f.User2)
                .ToListAsync();

            // get friends where the user is UserId2
            var friendships2 = await _context.Friends
                .Where(f => f.UserId2 == userId)
                .Include(f => f.User1)
                .ToListAsync();

            // Add User2 objects from friendships1
            friends.AddRange(friendships1.Select(f => f.User2));

            // Add User1 objects from friendships2
            friends.AddRange(friendships2.Select(f => f.User1));

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

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (targetUser == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }

            // users cannot send requests to theirselves
            if (targetUser.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Kendinize arkadaşlık isteği gönderemezsiniz.";
                return RedirectToAction(nameof(Friends));
            }

            // is already friend with the target user?
            var existingFriendship = await _context.Friends
                .AnyAsync(f => (f.UserId1 == currentUserId && f.UserId2 == targetUser.Id) ||
                              (f.UserId1 == targetUser.Id && f.UserId2 == currentUserId));

            if (existingFriendship)
            {
                TempData["ErrorMessage"] = "Bu kullanıcı zaten arkadaşınız.";
                return RedirectToAction(nameof(Friends));
            }

            // is already sent/received a request?
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

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == currentUserId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "İstek bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }

            request.Status = FriendRequestStatus.Accepted;

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

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == currentUserId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "İstek bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }

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

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.SenderId == currentUserId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "İstek bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }

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

            var friendship = await _context.Friends
                .FirstOrDefaultAsync(f =>
                    (f.UserId1 == currentUserId && f.UserId2 == friendId) ||
                    (f.UserId1 == friendId && f.UserId2 == currentUserId));

            if (friendship == null)
            {
                TempData["ErrorMessage"] = "Arkadaşlık bulunamadı.";
                return RedirectToAction(nameof(Friends));
            }

            _context.Friends.Remove(friendship);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Arkadaşlık silindi.";
            return RedirectToAction(nameof(Friends));
        }

        // GET: /FlashcardsView/UserSets/{id}
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> UserSets(int id, string? token = null)
        {
            Console.WriteLine($"UserSets action called for id: {id}, token provided: {!string.IsNullOrEmpty(token)}");
            
            if (!string.IsNullOrEmpty(token) && !Request.Headers.ContainsKey("Authorization"))
            {
                Console.WriteLine("Adding token to Authorization header");
                Request.Headers.Add("Authorization", $"Bearer {token}");
            }
            
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var areFriends = await _context.Friends
                .AnyAsync(f =>
                    (f.UserId1 == currentUserId && f.UserId2 == id) ||
                    (f.UserId1 == id && f.UserId2 == currentUserId));

            if (!areFriends && currentUserId != id)
            {
                TempData["ErrorMessage"] = "Bu kullanıcının setlerini görüntüleme izniniz yok.";
                return RedirectToAction(nameof(Friends));
            }

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
        public async Task<IActionResult> FriendSets(string? token = null)
        {
            Console.WriteLine("FriendSets (GET) action called");

            Console.WriteLine($"Token param provided: {!string.IsNullOrEmpty(token)}");
            Console.WriteLine($"Auth header present: {Request.Headers.ContainsKey("Authorization")}");

            if (!string.IsNullOrEmpty(token) && !Request.Headers.ContainsKey("Authorization"))
            {
                Console.WriteLine("Using token from query parameter for authentication in FriendSets view");
                HttpContext.Items["ManualToken"] = token;
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("No valid NameIdentifier claim found in User principal for FriendSets, trying to extract from token...");

                string extractedToken = null;

                string authHeader = Request.Headers["Authorization"];
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    extractedToken = authHeader.Substring("Bearer ".Length).Trim();
                    Console.WriteLine($"Found JWT in Authorization header: {extractedToken.Substring(0, Math.Min(20, extractedToken.Length))}...");
                }

                if (string.IsNullOrEmpty(extractedToken) && Request.Query.TryGetValue("token", out var queryToken))
                {
                    extractedToken = queryToken.ToString();
                    Console.WriteLine($"Found JWT in query parameter: {extractedToken.Substring(0, Math.Min(20, extractedToken.Length))}...");
                }

                if (string.IsNullOrEmpty(extractedToken) && Request.Cookies.TryGetValue("jwt", out string jwtFromCookie))
                {
                    extractedToken = jwtFromCookie;
                    Console.WriteLine($"Found JWT cookie in GET request: {extractedToken.Substring(0, Math.Min(20, extractedToken.Length))}...");
                }

                if (!string.IsNullOrEmpty(extractedToken))
                {
                    Console.WriteLine("Found token, using POST method version");
                    return await FriendSetsPost(extractedToken);
                }

                Console.WriteLine("No valid authentication found through any method, redirecting to login");
                return RedirectToAction("Login", "AuthView");
            }

            Console.WriteLine($"Using claim-based authentication for FriendSets, user ID: {userIdClaim}");
            var currentUserId = int.Parse(userIdClaim);

            var friendIds = new List<int>();

            var friendships1 = await _context.Friends
                .Where(f => f.UserId1 == currentUserId)
                .Select(f => f.UserId2)
                .ToListAsync();

            var friendships2 = await _context.Friends
                .Where(f => f.UserId2 == currentUserId)
                .Select(f => f.UserId1)
                .ToListAsync();

            friendIds.AddRange(friendships1);
            friendIds.AddRange(friendships2);

            var friendSets = await _context.FlashcardSets
                .Where(s => friendIds.Contains(s.UserId) &&
                           (s.Visibility == Visibility.Public || s.Visibility == Visibility.Friends))
                .Include(s => s.User)
                .Include(s => s.Flashcards)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(friendSets);
        }

        // POST: /FlashcardsView/Friends
        [HttpPost]
        [Route("FlashcardsView/Friends")]
        public async Task<IActionResult> FriendsPost(string token)
        {
            Console.WriteLine("Friends (POST) action called with token");

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("No token provided in POST");
                return RedirectToAction("Login", "AuthView");
            }

            Console.WriteLine($"Received token in Friends POST: {token.Substring(0, Math.Min(20, token.Length))}...");

            var jwtHandler = new JwtSecurityTokenHandler();
            var parsedToken = jwtHandler.ReadToken(token) as JwtSecurityToken;
            var userIdClaim = parsedToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                Console.WriteLine("Invalid token: no user ID claim found");
                return RedirectToAction("Login", "AuthView");
            }

            int userId = int.Parse(userIdClaim.Value);
            Console.WriteLine($"User ID from token: {userId}");

            var receivedRequests = await _context.FriendRequests
                .Where(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending)
                .Include(r => r.Sender)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var sentRequests = await _context.FriendRequests
                .Where(r => r.SenderId == userId)
                .Include(r => r.Receiver)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var friends = new List<User>();

            var friendships1 = await _context.Friends
                .Where(f => f.UserId1 == userId)
                .Include(f => f.User2)
                .ToListAsync();

            var friendships2 = await _context.Friends
                .Where(f => f.UserId2 == userId)
                .Include(f => f.User1)
                .ToListAsync();

            friends.AddRange(friendships1.Select(f => f.User2));

            friends.AddRange(friendships2.Select(f => f.User1));

            var model = new
            {
                ReceivedRequests = receivedRequests,
                SentRequests = sentRequests,
                Friends = friends
            };

            return View("Friends", model);
        }

        // POST: /FlashcardsView/FriendSets
        [HttpPost]
        [Route("FlashcardsView/FriendSets")]
        public async Task<IActionResult> FriendSetsPost(string token)
        {
            Console.WriteLine("FriendSets (POST) action called with token");

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("No token provided in POST");
                return RedirectToAction("Login", "AuthView");
            }

            Console.WriteLine($"Received token in FriendSets POST: {token.Substring(0, Math.Min(20, token.Length))}...");

            var jwtHandler = new JwtSecurityTokenHandler();
            var parsedToken = jwtHandler.ReadToken(token) as JwtSecurityToken;
            var userIdClaim = parsedToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                Console.WriteLine("Invalid token: no user ID claim found");
                return RedirectToAction("Login", "AuthView");
            }

            int currentUserId = int.Parse(userIdClaim.Value);
            Console.WriteLine($"User ID from token: {currentUserId}");

            var friendIds = new List<int>();

            var friendships1 = await _context.Friends
                .Where(f => f.UserId1 == currentUserId)
                .Select(f => f.UserId2)
                .ToListAsync();

            var friendships2 = await _context.Friends
                .Where(f => f.UserId2 == currentUserId)
                .Select(f => f.UserId1)
                .ToListAsync();

            friendIds.AddRange(friendships1);
            friendIds.AddRange(friendships2);

            var friendSets = await _context.FlashcardSets
                .Where(s => friendIds.Contains(s.UserId) &&
                           (s.Visibility == Visibility.Public || s.Visibility == Visibility.Friends))
                .Include(s => s.User)
                .Include(s => s.Flashcards)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View("FriendSets", friendSets);
        }

        // GET: api/friends/pending-requests
        [HttpGet("api/friends/pending-requests")]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> GetPendingFriendRequests()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

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

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (targetUser == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            if (targetUser.Id == currentUserId)
            {
                return BadRequest(new { message = "Kendinize arkadaşlık isteği gönderemezsiniz." });
            }

            var existingFriendship = await _context.Friends
                .AnyAsync(f => (f.UserId1 == currentUserId && f.UserId2 == targetUser.Id) ||
                              (f.UserId1 == targetUser.Id && f.UserId2 == currentUserId));

            if (existingFriendship)
            {
                return BadRequest(new { message = "Bu kullanıcı zaten arkadaşınız." });
            }

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

            request.Status = FriendRequestStatus.Accepted;

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

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == model.RequestId && r.ReceiverId == currentUserId);

            if (request == null)
            {
                return NotFound(new { message = "İstek bulunamadı." });
            }

            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arkadaşlık isteği reddedildi." });
        }

        // POST: api/friends/remove-friend
        [HttpPost("api/friends/remove-friend")]
        [Authentication.JwtAuthorize("Registered")]
        public async Task<IActionResult> ApiRemoveFriend([FromBody] ApiFriendIdModel model)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            Console.WriteLine($"[DEBUG] Remove friend API called - currentUserId: {currentUserId}, friendId: {model.FriendId}");

            var friendship = await _context.Friends
                .FirstOrDefaultAsync(f =>
                    (f.UserId1 == currentUserId && f.UserId2 == model.FriendId) ||
                    (f.UserId1 == model.FriendId && f.UserId2 == currentUserId));

            if (friendship == null)
            {
                return NotFound(new { message = "Arkadaşlık bulunamadı." });
            }

            _context.Friends.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arkadaşlık silindi." });
        }

        public class ApiRequestModel
        {
            public string Username { get; set; }
        }

        public class ApiRequestIdModel
        {
            public int RequestId { get; set; }
        }

        public class ApiFriendIdModel
        {
            public int FriendId { get; set; }
        }
    }
}
