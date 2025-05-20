using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using flashcardApp.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace flashcardApp.ViewComponents
{
    public class FriendSuggestionsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public FriendSuggestionsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int maxSuggestions = 5)
        {
            // Get the current user ID
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return View(new List<User>());
            }
            
            var currentUserId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            // Get current user's friends
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
            
            // Get users who have already sent or received friend requests from current user
            var requestUserIds = await _context.FriendRequests
                .Where(r => r.SenderId == currentUserId || r.ReceiverId == currentUserId)
                .Select(r => r.SenderId == currentUserId ? r.ReceiverId : r.SenderId)
                .ToListAsync();
            
            // Combine all IDs that should be excluded (friends, requests, and self)
            var excludedIds = new List<int>();
            excludedIds.AddRange(friendIds);
            excludedIds.AddRange(requestUserIds);
            excludedIds.Add(currentUserId);
            
            // Find users with most flashcard sets who are not already friends or have pending requests
            var suggestedUsers = await _context.Users
                .Where(u => !excludedIds.Contains(u.Id))
                .Include(u => u.FlashcardSets)
                .OrderByDescending(u => u.FlashcardSets.Count)
                .Take(maxSuggestions)
                .ToListAsync();
                
            return View(suggestedUsers);
        }
    }
}
