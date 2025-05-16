using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using flashcardApp.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;

namespace flashcardApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthDebugController : ControllerBase
    {
        [HttpGet("test")]
        public IActionResult TestAuth()
        {
            // Return debug information about the current request authentication
            var authInfo = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                AuthenticationType = User.Identity?.AuthenticationType,
                Name = User.Identity?.Name,
                Claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList(),
                Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                CookieKeys = Request.Cookies.Keys.ToList()
            };

            return Ok(authInfo);
        }

        [HttpGet("protected")]
        [JwtAuthorize]
        public IActionResult ProtectedEndpoint()
        {
            // This endpoint requires authentication
            var authInfo = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                AuthenticationType = User.Identity?.AuthenticationType,
                Name = User.Identity?.Name,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                UserRole = User.FindFirstValue(ClaimTypes.Role),
                Claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList()
            };

            return Ok(new { 
                message = "You have successfully accessed a protected endpoint!",
                authInfo = authInfo
            });
        }
        
        [HttpGet("cookie-status")]
        public IActionResult CookieStatus()
        {
            // Check if the JWT cookie exists and is valid
            if (Request.Cookies.TryGetValue("jwt", out string token))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    
                    var expiry = jwtToken.ValidTo;
                    var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
                    
                    return Ok(new {
                        hasCookie = true,
                        tokenValid = expiry > DateTime.UtcNow,
                        expiry = expiry,
                        tokenPreview = token.Substring(0, Math.Min(20, token.Length)) + "...",
                        claims = claims
                    });
                }
                catch (Exception ex)
                {
                    return Ok(new {
                        hasCookie = true,
                        tokenValid = false,
                        error = ex.Message,
                        tokenPreview = token.Substring(0, Math.Min(20, token.Length)) + "..."
                    });
                }
            }
            
            return Ok(new {
                hasCookie = false,
                message = "No JWT cookie found"
            });
        }
        
        [HttpGet("localstorage-test")]
        public ContentResult LocalStorageTest()
        {
            // Return a simple HTML page to test localStorage and cookie access
            var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>JWT Authentication Debug</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }
        pre { background: #f4f4f4; padding: 10px; border-radius: 5px; overflow: auto; }
        button { padding: 8px 15px; margin: 5px; cursor: pointer; }
        .success { color: green; }
        .error { color: red; }
    </style>
</head>
<body>
    <h1>JWT Authentication Debug Tool</h1>
    
    <div>
        <h2>LocalStorage Token</h2>
        <div id=""tokenStatus""></div>
        <button onclick=""checkToken()"">Check Token</button>
        <button onclick=""testEndpoint()"">Test Protected Endpoint</button>
        <button onclick=""testCookieEndpoint()"">Test Cookie Status</button>
    </div>
    
    <div>
        <h2>Set Cookies Manually</h2>
        <button onclick=""setCookie()"">Set JWT Cookie</button>
        <button onclick=""checkCookies()"">Check Cookies</button>
    </div>
    
    <div>
        <h2>Results</h2>
        <pre id=""results""></pre>
    </div>
    
    <script>
        function checkToken() {
            const token = localStorage.getItem('token');
            const tokenBackup = localStorage.getItem('jwtBackup');
            const tokenStatus = document.getElementById('tokenStatus');
            
            if (token) {
                tokenStatus.innerHTML = `<p class=""success"">Token found: ${token.substring(0, 20)}...</p>`;
                if (tokenBackup) {
                    tokenStatus.innerHTML += `<p class=""success"">Token backup found: ${tokenBackup.substring(0, 20)}...</p>`;
                }
            } else {
                tokenStatus.innerHTML = '<p class=""error"">No token found in localStorage</p>';
            }
            
            document.getElementById('results').innerText = 'Cookies: ' + document.cookie;
        }
        
        function setCookie() {
            const token = localStorage.getItem('token');
            if (!token) {
                document.getElementById('results').innerText = 'No token in localStorage to set as cookie';
                return;
            }
            
            const isLocalhost = window.location.hostname === 'localhost' || 
                             window.location.hostname === '127.0.0.1';
            const secureFlag = isLocalhost ? '' : '; Secure';
            const expires = '; expires=' + new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toUTCString();
            document.cookie = 'jwt=' + token + '; path=/; SameSite=Lax' + secureFlag + expires;
            
            checkCookies();
        }
        
        function checkCookies() {
            document.getElementById('results').innerText = 'Cookies: ' + document.cookie;
        }
        
        function testEndpoint() {
            const token = localStorage.getItem('token');
            if (!token) {
                document.getElementById('results').innerText = 'No token available for testing';
                return;
            }
            
            fetch('/api/AuthDebug/protected', {
                headers: {
                    'Authorization': 'Bearer ' + token
                }
            })
            .then(response => response.json())
            .then(data => {
                document.getElementById('results').innerText = JSON.stringify(data, null, 2);
            })
            .catch(error => {
                document.getElementById('results').innerText = 'Error: ' + error;
            });
        }
        
        function testCookieEndpoint() {
            fetch('/api/AuthDebug/cookie-status')
            .then(response => response.json())
            .then(data => {
                document.getElementById('results').innerText = JSON.stringify(data, null, 2);
            })
            .catch(error => {
                document.getElementById('results').innerText = 'Error: ' + error;
            });
        }
        
        // Run check token on page load
        checkToken();
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }
        
        [HttpGet("test-navigation")]
        public IActionResult TestNavigation()
        {
            // Return an HTML page that tests navigation to MySets
            var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Navigation Test</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }
        button { padding: 10px; margin: 5px; cursor: pointer; }
        .section { margin-bottom: 20px; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
        pre { background: #f4f4f4; padding: 10px; border-radius: 5px; }
    </style>
</head>
<body>
    <h1>MySets Navigation Test</h1>
    
    <div class=""section"">
        <h2>Token Information</h2>
        <button onclick=""checkToken()"">Check Token</button>
        <pre id=""tokenInfo"">Click button to check token</pre>
    </div>
    
    <div class=""section"">
        <h2>Test Navigation Methods</h2>
        <p>Test different ways to navigate to the MySets page:</p>
        
        <button onclick=""navigateWithQueryParam()"">Navigate with Query Parameter</button>
        <button onclick=""navigateWithProtectedLink()"">Use handleProtectedLink()</button>
        <button onclick=""navigateStandard()"">Standard Navigation</button>
    </div>
    
    <script>
        function checkToken() {
            const token = localStorage.getItem('token');
            if (!token) {
                document.getElementById('tokenInfo').textContent = 'No token found in localStorage!';
                return;
            }
            
            try {
                // Parse token
                const base64Url = token.split('.')[1];
                const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
                const payload = JSON.parse(atob(base64));
                
                let info = `Token found! Preview: ${token.substring(0, 20)}...\\n\\n`;
                info += `Expires: ${new Date(payload.exp * 1000).toLocaleString()}\\n`;
                info += `User ID: ${payload.nameid}\\n`;
                info += `Username: ${payload.unique_name || payload.name}\\n\\n`;
                info += `All claims: ${JSON.stringify(payload, null, 2)}`;
                
                document.getElementById('tokenInfo').textContent = info;
            } catch (e) {
                document.getElementById('tokenInfo').textContent = `Error parsing token: ${e.message}`;
            }
        }
        
        function navigateWithQueryParam() {
            const token = localStorage.getItem('token');
            if (!token) {
                alert('No token found in localStorage!');
                return;
            }
            
            window.location.href = '/FlashcardsView/MySets?token=' + encodeURIComponent(token);
        }
        
        function navigateWithProtectedLink() {
            if (typeof window.handleProtectedLink === 'function') {
                window.handleProtectedLink('/FlashcardsView/MySets');
            } else {
                alert('handleProtectedLink function not available. Make sure jwt-handler.js is loaded properly.');
            }
        }
        
        function navigateStandard() {
            window.location.href = '/FlashcardsView/MySets';
        }
        
        // Check token on load
        checkToken();
    </script>
    
    <!-- Load the jwt-handler.js file -->
    <script src=""/js/jwt-handler.js""></script>
</body>
</html>";

            return Content(html, "text/html");
        }
    }
}
