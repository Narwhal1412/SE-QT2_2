using B2BAgentPortal.Data;
using B2BAgentPortal.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace B2BAgentPortal.Pages;

public class OrdersModel : PageModel
{
    private readonly B2BContext _db;
    public OrdersModel(B2BContext db) => _db = db;

    public List<Order> Orders { get; set; } = new();

    public async Task OnGetAsync()
    {
        // AgentID comes from the login cookie: an agent only ever sees their own orders
        int agentId = int.Parse(User.FindFirst("AgentID")!.Value);

        Orders = await _db.Orders.AsNoTracking()
            .Where(o => o.AgentId == agentId)
            .Include(o => o.OrderDetails)            // needed to calculate each total
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}