using Microsoft.AspNetCore.Mvc;
using DotNetEnv;

namespace flashcardApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        // This is just for demonstration purposes to show the connection details
        // In a real application, you would never expose connection details like this
        [HttpGet("connection")]
        public IActionResult GetConnectionInfo()
        {
            return Ok(new
            {
                Server = Env.GetString("DB_SERVER"),
                Database = Env.GetString("DB_NAME"),
                User = Env.GetString("DB_USER"),
                Port = Env.GetString("DB_PORT"),
                // Not returning the password for security reasons
                UsingEnvFile = true
            });
        }
    }
}
