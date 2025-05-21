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
            System.Console.WriteLine($"JwtAuthorize OnAuthorization called for {context.HttpContext.Request.Path}");

            // check if user is authenticated
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                System.Console.WriteLine("User is already authenticated via standard mechanisms");
                // if a policy exists, standard authorization will handle it
                if (!string.IsNullOrEmpty(_policy))
                {
                    return;
                }

                // no policy, user is authenticated, its okay to go
                return;
            }
            // search for the JWT from multiple sources
            string token = null;

            // first check Authorization header
            string authHeader = context.HttpContext.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
                System.Console.WriteLine("Found token in Authorization header");
            }

            // check query parameter
            if (string.IsNullOrEmpty(token) && context.HttpContext.Request.Query.TryGetValue("token", out var queryToken))
            {
                token = queryToken;
                System.Console.WriteLine("Found token in query parameter");
                
                if (!string.IsNullOrEmpty(token))
                {
                    System.Console.WriteLine($"Setting Auth header from query token for {context.HttpContext.Request.Path}");
                    // add token to HttpContext.Items
                    context.HttpContext.Items["JwtFromQuery"] = token;
                }
            }

            // check cookies for JWT as last solution
            if (string.IsNullOrEmpty(token) && context.HttpContext.Request.Cookies.TryGetValue("jwt", out string cookieToken))
            {
                token = cookieToken;
                System.Console.WriteLine("Found token in cookie");
            }

            // auth debugging
            System.Console.WriteLine("Authorization header present: " + (authHeader != null));
            System.Console.WriteLine("Query parameter 'token' present: " + context.HttpContext.Request.Query.ContainsKey("token"));
            System.Console.WriteLine("Cookie 'jwt' present: " + context.HttpContext.Request.Cookies.ContainsKey("jwt"));

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // validate the token and create ClaimsPrincipal
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    // check if token is expired
                    var expiry = jwtToken.ValidTo;
                    if (expiry < DateTime.UtcNow)
                    {
                        System.Console.WriteLine("Token is expired");
                        context.Result = new RedirectToActionResult("Login", "AuthView", null);
                        return;
                    }

                    System.Console.WriteLine("Valid JWT found, setting up user principal");

                    // manually authenticate the user
                    var claims = jwtToken.Claims.ToList();

                    System.Console.WriteLine("Claims in token:");
                    foreach (var claim in claims)
                    {
                        System.Console.WriteLine($"  {claim.Type}: {claim.Value}");
                    }

                    // identity and principal
                    var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // set the user
                    context.HttpContext.User = principal;

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
                context.Result = new JsonResult(new { message = "Authentication required. No valid JWT token found." })
                {
                    StatusCode = 401
                };
                return;
            }
        }
    }
}
