using B2BAgentPortal.Data;
using B2BAgentPortal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace B2BAgentPortal.Pages;

public class IndexModel : PageModel
{
    private readonly B2BContext _db;
    public IndexModel(B2BContext db) => _db = db;

    public List<Item> Items { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }   // from ?Search=...

    public async Task OnGetAsync()
    {
        var q = _db.Items.AsNoTracking();                    // read-only, a bit faster
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(i => i.ItemName.Contains(Search));   // becomes SQL LIKE
        Items = await q.OrderBy(i => i.ItemName).ToListAsync();
    }
}