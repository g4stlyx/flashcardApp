using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using flashcardApp.Models;

namespace flashcardApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        // Check if user is logged in and is admin
        if (User.Identity.IsAuthenticated)
        {
            bool isAdmin = User.HasClaim(c => c.Type == "UserType" && c.Value == "Admin") ||
                          User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
                          
            ViewData["IsAdmin"] = isAdmin;
        }
        
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
