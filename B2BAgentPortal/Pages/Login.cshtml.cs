using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using B2BAgentPortal.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace B2BAgentPortal.Pages;

public class LoginModel : PageModel
{
    private readonly B2BContext _db;
    private readonly IConfiguration _config;
    public LoginModel(B2BContext db, IConfiguration config) { _db = db; _config = config; }

    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; set; }

    // SHA-256 as UPPERCASE hex: same format as Users.Password in the database
    private static string Hash(string pw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pw)));

    public async Task<IActionResult> OnPostAsync()
    {
        int max = _config.GetValue("MaxLoginAttempts", 5);   // same rule as the desktop app
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == Username);

        // Unknown user and wrong password give the same message
        if (user == null) { Error = "Invalid username or password."; return Page(); }
        if (user.Lock) { Error = "This account is locked."; return Page(); }

        if (user.Password != Hash(Password))
        {
            user.FailedAttempts++;                           // count the failure...
            if (user.FailedAttempts >= max) user.Lock = true; // ...and lock at the limit
            await _db.SaveChangesAsync();
            Error = "Invalid username or password.";
            return Page();
        }

        // Password is right, but only the Agent role may use this portal
        if (user.Role != "Agent")
        {
            Error = "Access denied: only Agent users may use this portal.";
            return Page();
        }

        user.FailedAttempts = 0;                             // success resets the counter
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role),
            new("AgentID", user.AgentId.ToString())          // used later to filter "My Orders"
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
        return RedirectToPage("/Index");
    }
}