using B2BAgentPortal.Data;
using B2BAgentPortal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace B2BAgentPortal.Pages;

public class InvoiceModel : PageModel
{
    private readonly B2BContext _db;
    public InvoiceModel(B2BContext db) => _db = db;

    public Order Order { get; set; } = null!;
    public decimal Total => Order.OrderDetails.Sum(d => d.Quantity * d.UnitAmount);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        int agentId = int.Parse(User.FindFirst("AgentID")!.Value);

        // The filter on AgentId is the security rule: asking for someone else's
        // invoice number finds nothing, so the answer is 404 instead of their data
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Agent)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(o => o.OrderId == id && o.AgentId == agentId);

        if (order == null) return NotFound();
        Order = order;
        return Page();
    }
}