using Microsoft.AspNetCore.Authorization;

namespace flashcardApp.Authentication
{
    public static class AuthorizationPolicies
    {
        public static void AddPolicies(AuthorizationOptions options)
        {
            // sadece adminler
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireClaim("UserType", "Admin"));
                
            // kayıtlı kullanıcılar ve adminler
            options.AddPolicy("Registered", policy =>
                policy.RequireClaim("UserType", "User", "Admin"));
        }
    }
}
