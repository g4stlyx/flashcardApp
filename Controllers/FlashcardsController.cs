using Microsoft.AspNetCore.Mvc;
using flashcardApp.Models;
using flashcardApp.Authentication;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace flashcardApp.Controllers
{
    [ApiController]
    [Route("api/flashcards")]
    public class FlashcardsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FlashcardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/flashcards/5
        [HttpGet("{id}")]
        public IActionResult GetFlashcard(int id)
        {
            var flashcard = _context.Flashcards.Find(id);
            
            if (flashcard == null)
            {
                return NotFound();
            }

            // Load related data
            _context.Entry(flashcard).Reference(f => f.FlashcardSet).Load();

            // Check if the set is private
            if (flashcard.FlashcardSet.Visibility == Visibility.Private)
            {
                // Only allow the owner to view flashcards in private sets
                if (!User.Identity.IsAuthenticated)
                {
                    return Forbid();
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (flashcard.FlashcardSet.UserId != userId)
                {
                    return Forbid();
                }
            }

            return Ok(new
            {
                flashcard.Id,
                flashcard.Term,
                flashcard.Definition,
                flashcard.ImageUrl,
                flashcard.ExampleSentence,
                flashcard.CreatedAt,
                flashcard.UpdatedAt
            });
        }

        // POST: api/flashcards
        [HttpPost]
        [JwtAuthorize("Registered")]
        public IActionResult CreateFlashcard([FromBody] FlashcardRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify user owns the set
            var set = _context.FlashcardSets.Find(request.SetId);
            if (set == null)
            {
                return NotFound("Flashcard set not found");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (set.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }            var flashcard = new Flashcard
            {
                SetId = request.SetId,
                Term = request.Term,
                Definition = request.Definition,
                ImageUrl = request.ImageUrl,
                ExampleSentence = request.ExampleSentence,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            try
            {
                _context.Flashcards.Add(flashcard);
                _context.SaveChanges();
                
                // Return a simplified version of the flashcard to avoid circular references
                return CreatedAtAction(nameof(GetFlashcard), new { id = flashcard.Id }, new
                {
                    flashcard.Id,
                    flashcard.SetId,
                    flashcard.Term,
                    flashcard.Definition,
                    flashcard.ImageUrl,
                    flashcard.ExampleSentence,
                    flashcard.CreatedAt,
                    flashcard.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.Error.WriteLine($"Error creating flashcard: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // PUT: api/flashcards/5
        [HttpPut("{id}")]
        [JwtAuthorize("Registered")]
        public IActionResult UpdateFlashcard(int id, [FromBody] FlashcardRequest request)
        {
            var flashcard = _context.Flashcards.Find(id);
            if (flashcard == null)
            {
                return NotFound();
            }

            // Load related data to check ownership
            _context.Entry(flashcard).Reference(f => f.FlashcardSet).Load();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (flashcard.FlashcardSet.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            flashcard.Term = request.Term;
            flashcard.Definition = request.Definition;
            flashcard.ImageUrl = request.ImageUrl;
            flashcard.ExampleSentence = request.ExampleSentence;
            flashcard.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return NoContent();
        }

        // DELETE: api/flashcards/5
        [HttpDelete("{id}")]
        [JwtAuthorize("Registered")]
        public IActionResult DeleteFlashcard(int id)
        {
            var flashcard = _context.Flashcards.Find(id);
            if (flashcard == null)
            {
                return NotFound();
            }

            // Load related data to check ownership
            _context.Entry(flashcard).Reference(f => f.FlashcardSet).Load();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (flashcard.FlashcardSet.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _context.Flashcards.Remove(flashcard);
            _context.SaveChanges();

            return NoContent();
        }

        // GET: api/flashcards/by-set/5
        [HttpGet("by-set/{setId}")]
        public IActionResult GetFlashcardsBySet(int setId)
        {
            var set = _context.FlashcardSets.Find(setId);
            if (set == null)
            {
                return NotFound("Flashcard set not found");
            }

            // Check if the set is private
            if (set.Visibility == Visibility.Private)
            {
                // Only allow the owner to view flashcards in private sets
                if (!User.Identity.IsAuthenticated)
                {
                    return Forbid();
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (set.UserId != userId)
                {
                    return Forbid();
                }
            }

            _context.Entry(set).Collection(s => s.Flashcards).Load();

            var flashcards = set.Flashcards.Select(f => new
            {
                f.Id,
                f.Term,
                f.Definition,
                f.ImageUrl,
                f.ExampleSentence,
                f.CreatedAt,
                f.UpdatedAt
            }).ToList();

            return Ok(flashcards);
        }
    }
    public class FlashcardRequest
    {
        [Required]
        public int SetId { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "Term cannot be longer than 255 characters")]
        public string Term { get; set; }

        [Required]
        public string Definition { get; set; }

        public string? ImageUrl { get; set; }

        public string? ExampleSentence { get; set; }
    }
}
