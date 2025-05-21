using Microsoft.AspNetCore.Mvc;
using flashcardApp.Models.Authentication;

namespace flashcardApp.Controllers
{
    public class AuthViewController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }
        
        public IActionResult Logout()
        {
            return RedirectToAction("Login");
        }
    }
}
