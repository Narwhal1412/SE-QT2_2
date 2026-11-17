using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace B2BAgentPortal.Pages;

public class LogoutModel : PageModel
{
    // Typing /Logout in the address bar does nothing harmful
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync();        // deletes the login cookie
        return RedirectToPage("/Login");
    }
}