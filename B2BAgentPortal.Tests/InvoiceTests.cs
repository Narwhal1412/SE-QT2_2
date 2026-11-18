using B2BAgentPortal.Models;
using B2BAgentPortal.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace B2BAgentPortal.Tests;

/// <summary>Invoice total calculation and the rule that an agent can only see their own invoices.</summary>
public class InvoiceTests
{
    [Fact]
    public void Total_IsTheSumOfQuantityTimesUnitAmount()
    {
        var model = new InvoiceModel(null!)
        {
            Order = new Order
            {
                OrderDetails =
                {
                    new OrderDetail { Quantity = 2, UnitAmount = 10_000m },   // 20,000
                    new OrderDetail { Quantity = 3, UnitAmount = 2_500m },    //  7,500
                }
            }
        };

        Assert.Equal(27_500m, model.Total);
    }

    [Fact]
    public void Total_OfAnOrderWithoutLines_IsZero()
    {
        var model = new InvoiceModel(null!) { Order = new Order() };

        Assert.Equal(0m, model.Total);
    }

    private static InvoiceModel ModelFor(TestDb test, int agentId) => new(test.NewContext())
    {
        PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("AgentID", agentId.ToString()) }, "test"))
            }
        }
    };

    [Fact]
    public async Task Invoice_OfAnotherAgent_ReturnsNotFound()
    {
        using var test = new TestDb();
        int owner = test.AddAgent("Owner");
        int intruder = test.AddAgent("Intruder");
        int item = test.AddItem("Rice 5kg", 100_000m, 10);
        int orderId = test.AddOrder(owner, (item, 1, 100_000m));

        var result = await ModelFor(test, intruder).OnGetAsync(orderId);

        Assert.IsType<NotFoundResult>(result);   // never leaks another agent's data
    }

    [Fact]
    public async Task Invoice_OfTheLoggedInAgent_IsShownWithCorrectTotal()
    {
        using var test = new TestDb();
        int owner = test.AddAgent("Owner");
        int rice = test.AddItem("Rice 5kg", 100_000m, 10);
        int salt = test.AddItem("Salt 500g", 5_000m, 10);
        int orderId = test.AddOrder(owner, (rice, 2, 100_000m), (salt, 4, 5_000m));

        var model = ModelFor(test, owner);
        var result = await model.OnGetAsync(orderId);

        Assert.IsType<PageResult>(result);
        Assert.Equal(220_000m, model.Total);     // 2*100,000 + 4*5,000
    }
}
