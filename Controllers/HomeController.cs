using System;
using System.Diagnostics;
using System.Linq;
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
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Users()
    {
        return View();
    }

    public IActionResult TestDatabase()
    {
        try
        {
            var userCount = _context.Users.Count();
            ViewBag.Message = $"Database connection successful! Found {userCount} users.";
            ViewBag.Success = true;
        }
        catch (Exception ex)
        {
            ViewBag.Message = $"Error connecting to database: {ex.Message}";
            ViewBag.Success = false;
            ViewBag.Error = ex.ToString();
        }
        
        return View();
    }

    public IActionResult DatabaseInfo()
    {
        ViewBag.ConnectionInfo = new
        {
            Server = DotNetEnv.Env.GetString("DB_SERVER"),
            Database = DotNetEnv.Env.GetString("DB_NAME"),
            User = DotNetEnv.Env.GetString("DB_USER"),
            Port = DotNetEnv.Env.GetString("DB_PORT")
        };
        
        try
        {
            ViewBag.UserCount = _context.Users.Count();
            ViewBag.Success = true;
        }
        catch (Exception ex)
        {
            ViewBag.Success = false;
            ViewBag.Error = ex.Message;
        }
        
        return View();
    }

    public IActionResult AuthDebug()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
