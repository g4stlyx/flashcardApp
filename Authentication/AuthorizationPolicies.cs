using Microsoft.AspNetCore.Authorization;

namespace flashcardApp.Authentication
{
    public static class AuthorizationPolicies
    {
        public static void AddPolicies(AuthorizationOptions options)
        {
            // Admin only policy
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireClaim("UserType", "Admin"));
                
            // Registered users (both users and admins)
            options.AddPolicy("Registered", policy =>
                policy.RequireClaim("UserType", "User", "Admin"));
        }
    }
}
