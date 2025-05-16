using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace flashcardApp.Authentication
{
    public class JwtAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string _policy;
        
        public JwtAuthorizeAttribute()
        {
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
            _policy = null;
        }

        public JwtAuthorizeAttribute(string policy) : this()
        {
            Policy = policy;
            _policy = policy;
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Standard authorization will be handled by the base AuthorizeAttribute
            // This is just an additional check for JWT tokens in various locations

            System.Console.WriteLine($"JwtAuthorize OnAuthorization called for {context.HttpContext.Request.Path}");

            // If user is already authenticated, we're good to go
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                System.Console.WriteLine("User is already authenticated via standard mechanisms");
                // If a policy is specified, let the standard authorization handle it
                if (!string.IsNullOrEmpty(_policy))
                {
                    return;
                }

                // No policy, user is authenticated, we're good to go
                return;
            }
            // Try to find the JWT from multiple sources
            string token = null;

            // First check Authorization header (preferred method)
            string authHeader = context.HttpContext.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
                System.Console.WriteLine("Found token in Authorization header");
            }

            // Check query parameter next (our main fallback)
            if (string.IsNullOrEmpty(token) && context.HttpContext.Request.Query.TryGetValue("token", out var queryToken))
            {
                token = queryToken;
                System.Console.WriteLine("Found token in query parameter");
            }

            // Then check cookies if we didn't find a token yet (last resort since cookies aren't working well)
            if (string.IsNullOrEmpty(token) && context.HttpContext.Request.Cookies.TryGetValue("jwt", out string cookieToken))
            {
                token = cookieToken;
                System.Console.WriteLine("Found token in cookie");
            }

            // Log all available auth details for debugging
            System.Console.WriteLine("Authorization header present: " + (authHeader != null));
            System.Console.WriteLine("Query parameter 'token' present: " + context.HttpContext.Request.Query.ContainsKey("token"));
            System.Console.WriteLine("Cookie 'jwt' present: " + context.HttpContext.Request.Cookies.ContainsKey("jwt"));

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // Manually validate the token and create a ClaimsPrincipal
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    // Check if token is expired
                    var expiry = jwtToken.ValidTo;
                    if (expiry < DateTime.UtcNow)
                    {
                        System.Console.WriteLine("Token is expired");
                        context.Result = new RedirectToActionResult("Login", "AuthView", null);
                        return;
                    }

                    // Log that we found a valid token
                    System.Console.WriteLine("Valid JWT found, setting up user principal");

                    // Create a ClaimsPrincipal and manually authenticate the user
                    var claims = jwtToken.Claims.ToList();

                    // Log all claims for debugging
                    System.Console.WriteLine("Claims in token:");
                    foreach (var claim in claims)
                    {
                        System.Console.WriteLine($"  {claim.Type}: {claim.Value}");
                    }

                    // Create identity and principal
                    var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // Set the user
                    context.HttpContext.User = principal;

                    // Let the request proceed now that we've manually authenticated
                    return;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error validating JWT: {ex.Message}");
                    context.Result = new RedirectToActionResult("Login", "AuthView", null);
                    return;
                }
            }
            else
            {
                System.Console.WriteLine("No JWT found in any location");
                // Return a more specific error instead of just redirecting
                context.Result = new JsonResult(new { message = "Authentication required. No valid JWT token found." })
                {
                    StatusCode = 401
                };
                return;
            }
        }
    }
}
