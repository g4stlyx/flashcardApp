using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using flashcardApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace flashcardApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // put token in redirects
        private IActionResult RedirectWithToken(string action, object routeValues = null)
        {
            string token = Request.Query["token"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                // Try to extract token from Authorization header
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    token = authHeader.Substring(7);
                }
            }
            
            if (!string.IsNullOrEmpty(token))
            {
                if (routeValues == null)
                {
                    routeValues = new { token };
                }
                else
                {
                    var type = routeValues.GetType();
                    var properties = type.GetProperties();
                    var dictionary = new System.Collections.Generic.Dictionary<string, object>();
                    foreach (var property in properties)
                    {
                        dictionary[property.Name] = property.GetValue(routeValues);
                    }
                    dictionary["token"] = token;
                    routeValues = dictionary;
                }
            }
            
            return RedirectToAction(action, routeValues);
        }
        public async Task<IActionResult> ManageUsers(string searchTerm = null)
        {
            var usersQuery = _context.Users
                .Include(u => u.FlashcardSets)
                    .ThenInclude(fs => fs.Flashcards)
                .Include(u => u.SentFriendRequests)
                .Include(u => u.ReceivedFriendRequests)
                .AsQueryable();

            // apply search filter if it exists
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.Username.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchTerm))
                );

                ViewData["SearchTerm"] = searchTerm;
            }

            // statistics
            var users = await usersQuery.ToListAsync();
            var totalAdmins = users.Count(u => u.IsAdmin);
            var totalRegularUsers = users.Count - totalAdmins;
            var totalSets = users.Sum(u => u.FlashcardSets.Count);
            var totalFlashcards = users.Sum(u => u.FlashcardSets.Sum(fs => fs.Flashcards?.Count ?? 0));
            var latestUserRegistration = users.OrderByDescending(u => u.CreatedAt).FirstOrDefault()?.CreatedAt;

            // add statistics to ViewBag
            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalAdmins = totalAdmins;
            ViewBag.TotalRegularUsers = totalRegularUsers;
            ViewBag.TotalSets = totalSets;
            ViewBag.TotalFlashcards = totalFlashcards;
            ViewBag.LatestUserRegistration = latestUserRegistration;

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.FlashcardSets)
                    .ThenInclude(fs => fs.Flashcards)
                .Include(u => u.SentFriendRequests)
                    .ThenInclude(fr => fr.Receiver)
                .Include(u => u.ReceivedFriendRequests)
                    .ThenInclude(fr => fr.Sender)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int userId, string token)
        {
            Console.WriteLine($"[AdminController] DeleteUser called for userId: {userId}");
            Console.WriteLine($"[AdminController] Token provided: {(token != null ? "YES, length: " + token.Length : "NO")}");
            
            // header debugging
            if (Request.Headers.ContainsKey("Authorization"))
            {
                Console.WriteLine($"[AdminController] Authorization header found: {Request.Headers["Authorization"]}");
            }
            else
            {
                Console.WriteLine($"[AdminController] No Authorization header found");
            }
            
            // if token -> add to the auth header
            if (!string.IsNullOrEmpty(token) && !Request.Headers.ContainsKey("Authorization"))
            {
                Console.WriteLine($"[AdminController] Adding token to Authorization header");
                Request.Headers.Append("Authorization", $"Bearer {token}");
            }
            
            var user = await _context.Users
                .Include(u => u.FlashcardSets)
                    .ThenInclude(fs => fs.Flashcards)
                .Include(u => u.SentFriendRequests)
                .Include(u => u.ReceivedFriendRequests)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null)
            {
                Console.WriteLine($"[AdminController] User with ID {userId} not found");
                return NotFound();
            }
            
            // users cannot delete theirselves
            var currentUserIdClaim = User.FindFirst("nameid");
            if (currentUserIdClaim != null && int.TryParse(currentUserIdClaim.Value, out int currentUserId) && currentUserId == userId)
            {
                Console.WriteLine($"[AdminController] Attempted to delete own account: {currentUserId}");
                TempData["ErrorMessage"] = "Kendi hesabınızı silemezsiniz!";
                return RedirectWithToken(nameof(ManageUsers));
            }
            
            // delete all flashcards and sets with the user
            foreach (var set in user.FlashcardSets.ToList())
            {
                _context.Flashcards.RemoveRange(set.Flashcards);
                _context.FlashcardSets.Remove(set);
            }

            // delete all friend stuff too
            _context.FriendRequests.RemoveRange(user.SentFriendRequests);
            _context.FriendRequests.RemoveRange(user.ReceivedFriendRequests);
            
            var friendships = await _context.Friends
                .Where(f => f.UserId1 == userId || f.UserId2 == userId)
                .ToListAsync();
            _context.Friends.RemoveRange(friendships);
            Console.WriteLine($"[AdminController] Removed {friendships.Count} friendships");
            
            // finally delete the user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                Console.WriteLine($"[AdminController] AJAX request detected, returning JSON success");
                return Json(new { success = true, message = $"{user.Username} kullanıcısı başarıyla silindi." });
            }
            else
            {
                Console.WriteLine($"[AdminController] Standard form submission, redirecting with token");
                TempData["SuccessMessage"] = $"{user.Username} kullanıcısı başarıyla silindi.";
                return RedirectWithToken(nameof(ManageUsers));
            }
        }
    }
}
