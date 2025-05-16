using Microsoft.EntityFrameworkCore;
using flashcardApp.Models;
using DotNetEnv;
using flashcardApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Add authorization policies
builder.Services.AddAuthorization(options => 
{
    flashcardApp.Authentication.AuthorizationPolicies.AddPolicies(options);
});

// Configure JWT Authentication
var jwtSecret = Env.GetString("JWT_SECRET");
var jwtIssuer = Env.GetString("JWT_ISSUER");
var jwtAudience = Env.GetString("JWT_AUDIENCE");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
    // This allows accepting JWT from the Authorization header as well as cookies
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context => 
        {
            // Log all request details for debugging
            Console.WriteLine($"OnMessageReceived: {context.Request.Path}");
            Console.WriteLine("Headers:");
            foreach (var header in context.Request.Headers)
            {
                Console.WriteLine($"  {header.Key}: {header.Value}");
            }
            Console.WriteLine("Cookies:");
            foreach (var cookie in context.Request.Cookies)
            {
                Console.WriteLine($"  {cookie.Key}: {cookie.Value.Substring(0, Math.Min(20, cookie.Value.Length))}...");
            }
            
            // First check if the token is already in the Authorization header
            string token = null;
            string authHeader = context.Request.Headers["Authorization"];
            
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
                Console.WriteLine("Found token in Authorization header");
            }
            
            // If no token in auth header, check cookie
            if (string.IsNullOrEmpty(token) && context.Request.Cookies.ContainsKey("jwt"))
            {
                token = context.Request.Cookies["jwt"];
                Console.WriteLine("Found token in cookie");
            }
            
            // If still no token, check query string as last resort
            if (string.IsNullOrEmpty(token) && context.Request.Query.ContainsKey("token"))
            {
                token = context.Request.Query["token"];
                Console.WriteLine("Found token in query string");
            }
            
            if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine($"Setting token: {token.Substring(0, Math.Min(20, token.Length))}...");
                context.Token = token;
            }
            else
            {
                Console.WriteLine("No token found in request");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"Challenge triggered: {context.AuthenticateFailure?.Message ?? "No specific failure"}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token successfully validated");
            return Task.CompletedTask;
        }
    };
});

// Build connection string from environment variables
var server = Env.GetString("DB_SERVER");
var dbName = Env.GetString("DB_NAME");
var user = Env.GetString("DB_USER");
var password = Env.GetString("DB_PASSWORD");
var port = Env.GetString("DB_PORT");

var connectionString = $"Server={server};Database={dbName};User={user};Password={password};Port={port};";

// Add MySQL Database Connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
