using Microsoft.AspNetCore.Mvc;
using flashcardApp.Models;
using flashcardApp.Authentication;
using flashcardApp.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace flashcardApp.Controllers
{
    [ApiController]
    [Route("api/flashcard-sets")]
    public class FlashcardSetsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FlashcardSetsController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: api/flashcard-sets/test-auth
        [HttpGet("test-auth")]
        [JwtAuthorize("Registered")]
        public IActionResult TestAuth()
        {
            try
            {
                var isAuthenticated = User.Identity.IsAuthenticated;
                var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                return Ok(new
                {
                    IsAuthenticated = isAuthenticated,
                    UserId = userId,
                    Claims = claims
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // GET: api/flashcard-sets
        [HttpGet]
        public IActionResult GetPublicSets()
        {
            var publicSets = _context.FlashcardSets
                .Where(s => s.Visibility == Visibility.Public)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.CoverImageUrl,
                    s.CreatedAt,
                    s.UpdatedAt,
                    Username = s.User.Username,
                    UserId = s.UserId,
                    FlashcardCount = s.Flashcards.Count,
                    ViewCount = s.Views.Count
                })
                .ToList();

            return Ok(publicSets);
        }

        // GET: api/flashcard-sets/5
        [HttpGet("{id}")]
        public IActionResult GetSet(int id)
        {
            var set = _context.FlashcardSets.Find(id);

            if (set == null)
            {
                return NotFound();
            }

            // is the set private?
            if (set.Visibility == Visibility.Private)
            {
                // only owners
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || set.UserId != int.Parse(userId))
                {
                    return Forbid();
                }
            }

            _context.Entry(set).Reference(s => s.User).Load();
            _context.Entry(set).Collection(s => s.Flashcards).Load();
            _context.Entry(set).Collection(s => s.Tags).Load();

            // increase view count if user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _context.SetViews.Add(new SetView
                {
                    UserId = userId,
                    SetId = id,
                    ViewedAt = DateTime.UtcNow
                });
                _context.SaveChanges();
            }

            return Ok(new
            {
                set.Id,
                set.Title,
                set.Description,
                set.Visibility,
                set.CoverImageUrl,
                set.CreatedAt,
                set.UpdatedAt,
                Username = set.User.Username,
                UserId = set.UserId,
                Flashcards = set.Flashcards.Select(f => new
                {
                    f.Id,
                    f.Term,
                    f.Definition,
                    f.ImageUrl,
                    f.ExampleSentence
                }),
                Tags = set.Tags.Select(t => t.TagName)
            });
        }

        // POST: api/flashcard-sets
        [HttpPost]
        [JwtAuthorize("Registered")]
        public IActionResult CreateSet([FromBody] FlashcardSetRequest request)
        {
            Console.WriteLine("CreateSet request received from: " + HttpContext.Connection.RemoteIpAddress);
            Console.WriteLine("Content-Type: " + Request.ContentType);

            foreach (var header in Request.Headers)
            {
                if (header.Key == "Authorization")
                {
                    Console.WriteLine($"Header {header.Key}: {(header.Value.ToString().Length > 20 ? header.Value.ToString().Substring(0, 20) + "..." : header.Value)}");
                }
                else
                {
                    Console.WriteLine($"Header {header.Key}: {header.Value}");
                }
            }

            Console.WriteLine("CreateSet called with request: " + (request != null ?
                $"Title={request.Title}, Description={request.Description?.Substring(0, Math.Min(20, request.Description?.Length ?? 0))}" :
                "null"));

            Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
            if (User.Identity.IsAuthenticated)
            {
                Console.WriteLine($"User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            }

            Console.WriteLine("ModelState.IsValid: " + ModelState.IsValid);

            foreach (var state in ModelState)
            {
                Console.WriteLine($"Key: {state.Key}, Value: {state.Value.AttemptedValue}, Validation State: {state.Value.ValidationState}");
                foreach (var error in state.Value.Errors)
                {
                    Console.WriteLine($"  Error message: {error.ErrorMessage}");
                    if (error.Exception != null)
                    {
                        Console.WriteLine($"  Exception: {error.Exception.Message}");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState invalid: " + string.Join(", ",
                    ModelState.Where(kvp => kvp.Value.Errors.Count > 0)
                    .Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage))}")));
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = ModelState.Where(kvp => kvp.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                        )
                });
            }
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                Console.WriteLine($"User ID from claims: {userId}");

                Console.WriteLine($"Visibility value: {(int)request.Visibility} ({request.Visibility})");

                var set = new FlashcardSet
                {
                    UserId = userId,
                    Title = request.Title,
                    Description = request.Description,
                    Visibility = request.Visibility,
                    CoverImageUrl = request.CoverImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                string visibilityString = Enum.GetName(typeof(Visibility), request.Visibility)?.ToLower();
                Console.WriteLine($"Converted visibility string: {visibilityString}");

                _context.FlashcardSets.Add(set);
                _context.SaveChanges();
                Console.WriteLine($"Successfully created set with ID: {set.Id}");

                return CreatedAtAction(nameof(GetSet), new { id = set.Id }, set);
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error creating set: {dbEx.Message}");
                Console.WriteLine($"Inner exception: {dbEx.InnerException?.Message}");

                if (dbEx.InnerException?.Message?.Contains("truncated") == true)
                {
                    return BadRequest(new
                    {
                        message = "Invalid value for one or more fields. Please check the visibility setting.",
                        detail = dbEx.InnerException.Message
                    });
                }

                return BadRequest(new { message = "Database error while creating set", detail = dbEx.InnerException?.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating set: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/flashcard-sets/5
        [HttpPut("{id}")]
        [JwtAuthorize("Registered")]
        public IActionResult UpdateSet(int id, [FromBody] FlashcardSetRequest request)
        {
            var set = _context.FlashcardSets.Find(id);
            if (set == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (set.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            set.Title = request.Title;
            set.Description = request.Description;
            set.Visibility = request.Visibility;
            set.CoverImageUrl = request.CoverImageUrl;
            set.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return NoContent();
        }

        // DELETE: api/flashcard-sets/5
        [HttpDelete("{id}")]
        [JwtAuthorize("Registered")]
        public IActionResult DeleteSet(int id)
        {
            var set = _context.FlashcardSets.Find(id);
            if (set == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (set.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _context.FlashcardSets.Remove(set);
            _context.SaveChanges();

            return NoContent();
        }

        // GET: api/flashcard-sets/my-sets
        [HttpGet("my-sets")]
        [JwtAuthorize("Registered")]
        public IActionResult GetMySets()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var sets = _context.FlashcardSets
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Visibility,
                    s.CoverImageUrl,
                    s.CreatedAt,
                    s.UpdatedAt,
                    FlashcardCount = s.Flashcards.Count,
                    ViewCount = s.Views.Count
                })
                .ToList();

            return Ok(sets);
        }
    }    public class FlashcardSetRequest
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = null!;
        
        public string? Description { get; set; }
        
        public Visibility Visibility { get; set; } = Visibility.Public;
        
        public string? CoverImageUrl { get; set; }
    }
}
