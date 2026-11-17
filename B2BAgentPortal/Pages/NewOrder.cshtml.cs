using B2BAgentPortal.Data;
using B2BAgentPortal.Models;
using B2BAgentPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace B2BAgentPortal.Pages;

// One row of the form (the "detail" part)
public class LineInput
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class NewOrderModel : PageModel
{
    private readonly B2BContext _db;
    private readonly OrderService _orders;
    public NewOrderModel(B2BContext db, OrderService orders) { _db = db; _orders = orders; }

    public List<Item> Items { get; set; } = new();                 // fills the dropdown
    [BindProperty] public List<LineInput> Lines { get; set; } = new();

    public async Task OnGetAsync() => await LoadItems();

    public async Task<IActionResult> OnPostAsync()
    {
        // The agent id comes from the login cookie, never from the form,
        // so nobody can place an order in someone else's name
        int agentId = int.Parse(User.FindFirst("AgentID")!.Value);

        var result = await _orders.PlaceOrderAsync(
            agentId, Lines.Select(l => new OrderLine(l.ItemId, l.Quantity)));

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error!);           // shown in the red summary
            await LoadItems();
            return Page();                                         // keeps the lines the agent typed
        }

        TempData["Message"] = $"Order #{result.OrderId} was placed successfully.";
        return RedirectToPage();                                   // avoids a double-submit on refresh
    }

    private async Task LoadItems() =>
        Items = await _db.Items.AsNoTracking().OrderBy(i => i.ItemName).ToListAsync();
}