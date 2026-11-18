using B2BAgentPortal.Services;
using Microsoft.EntityFrameworkCore;

namespace B2BAgentPortal.Tests;

/// <summary>
/// Core business logic of the Agent Portal: merging order lines and the order transaction
/// (create order + details + deduct stock, all-or-nothing).
/// </summary>
public class OrderServiceTests
{
    // =====================================================================
    // Pure logic: OrderService.MergeLines (no database)
    // =====================================================================

    [Fact]
    public void MergeLines_SameItemTwice_SumsQuantitiesIntoOneLine()
    {
        var result = OrderService.MergeLines(new[] { new OrderLine(1, 2), new OrderLine(1, 3) });

        var line = Assert.Single(result);
        Assert.Equal(1, line.ItemId);
        Assert.Equal(5, line.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void MergeLines_ZeroOrNegativeQuantity_IsDropped(int quantity)
    {
        var result = OrderService.MergeLines(new[] { new OrderLine(1, quantity), new OrderLine(2, 1) });

        var line = Assert.Single(result);
        Assert.Equal(2, line.ItemId);
    }

    [Fact]
    public void MergeLines_EmptyInput_ReturnsEmptyList()
    {
        Assert.Empty(OrderService.MergeLines(Array.Empty<OrderLine>()));
    }

    [Fact]
    public void MergeLines_DistinctItems_KeepsAllLinesInOrder()
    {
        var result = OrderService.MergeLines(new[] { new OrderLine(7, 1), new OrderLine(3, 2), new OrderLine(5, 3) });

        Assert.Equal(new[] { 7, 3, 5 }, result.Select(l => l.ItemId));
        Assert.Equal(new[] { 1, 2, 3 }, result.Select(l => l.Quantity));
    }

    [Fact]
    public void MergeLines_DuplicateWithNonPositiveLine_IgnoresOnlyTheBadLine()
    {
        // 4 + 0 for the same item: the empty line must not break or reduce the total
        var result = OrderService.MergeLines(new[] { new OrderLine(1, 4), new OrderLine(1, 0) });

        Assert.Equal(4, Assert.Single(result).Quantity);
    }

    // =====================================================================
    // Order transaction: OrderService.PlaceOrderAsync (real SQL engine)
    // =====================================================================

    [Fact]
    public async Task PlaceOrder_EnoughStock_CreatesOrderDetailsAndDeductsStock()
    {
        using var test = new TestDb();
        int agent = test.AddAgent();
        int rice = test.AddItem("Rice 5kg", 100_000m, 50);
        int salt = test.AddItem("Salt 500g", 5_000m, 20);

        using var db = test.NewContext();
        var result = await new OrderService(db).PlaceOrderAsync(agent,
            new[] { new OrderLine(rice, 10), new OrderLine(salt, 4) });

        Assert.True(result.Success);
        Assert.NotNull(result.OrderId);
        Assert.Null(result.Error);

        // Stock deducted for both items
        Assert.Equal(40, test.StockOf(rice));
        Assert.Equal(16, test.StockOf(salt));

        // Master + details really saved
        using var check = test.NewContext();
        var order = await check.Orders.AsNoTracking().Include(o => o.OrderDetails)
                               .SingleAsync(o => o.OrderId == result.OrderId);
        Assert.Equal(agent, order.AgentId);
        Assert.Equal("Pending", order.OrderStatus);
        Assert.Equal(2, order.OrderDetails.Count);
    }

    [Fact]
    public async Task PlaceOrder_InsufficientStock_IsBlockedAndNothingIsSaved()
    {
        using var test = new TestDb();
        int agent = test.AddAgent();
        int salt = test.AddItem("Salt 500g", 5_000m, 5);

        using var db = test.NewContext();
        var result = await new OrderService(db).PlaceOrderAsync(agent, new[] { new OrderLine(salt, 6) });

        Assert.False(result.Success);
        Assert.Null(result.OrderId);
        Assert.Contains("Insufficient stock", result.Error);

        Assert.Equal(5, test.StockOf(salt));                 // stock untouched
        using var check = test.NewContext();
        Assert.Empty(await check.Orders.AsNoTracking().ToListAsync());        // order rolled back
        Assert.Empty(await check.OrderDetails.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task PlaceOrder_SecondLineOutOfStock_RollsBackTheFirstLineToo()
    {
        using var test = new TestDb();
        int agent = test.AddAgent();
        int rice = test.AddItem("Rice 5kg", 100_000m, 50);
        int salt = test.AddItem("Salt 500g", 5_000m, 2);

        using var db = test.NewContext();
        var result = await new OrderService(db).PlaceOrderAsync(agent,
            new[] { new OrderLine(rice, 10), new OrderLine(salt, 3) });   // 2nd line fails

        Assert.False(result.Success);
        // Atomicity: the rice deduction that already happened must be undone
        Assert.Equal(50, test.StockOf(rice));
        Assert.Equal(2, test.StockOf(salt));
        using var check = test.NewContext();
        Assert.Empty(await check.Orders.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task PlaceOrder_ExactlyAllRemainingStock_Succeeds_AndStockBecomesZero()
    {
        using var test = new TestDb();
        int agent = test.AddAgent();
        int item = test.AddItem("Sugar 1kg", 20_000m, 7);

        using var db = test.NewContext();
        var result = await new OrderService(db).PlaceOrderAsync(agent, new[] { new OrderLine(item, 7) });

        Assert.True(result.Success);
        Assert.Equal(0, test.StockOf(item));
    }

    [Fact]
    public async Task PlaceOrder_DuplicateLines_AreMergedAndDeductedTogether()
    {
        using var test = new TestDb();
        int agent = test.AddAgent();
        int item = test.AddItem("Sugar 1kg", 20_000m, 10);

        using var db = test.NewContext();
        var result = await new OrderService(db).PlaceOrderAsync(agent,
            new[] { new OrderLine(item, 3), new OrderLine(item, 4) });

        Assert.True(result.Success);
        Assert.Equal(3, test.StockOf(item));                  // 10 - (3 + 4)

        using var check = test.NewContext();
        var detail = await check.OrderDetails.AsNoTracking().SingleAsync();   // one row, not two
        Assert.Equal(7, detail.Quantity);
    }

    [Fact]
    public async Task PlaceOrder_DuplicateLinesExceedingStock_AreBlocked()
    {
        // Each line alone fits (6 <= 10) but together they do not (12 > 10)
        using var test = new TestDb();
        int agent = test.AddAgent();
        int item = test.AddItem("Sugar 1kg", 20_000m, 10);

        using var db = test.NewContext();
        var result = await new OrderService(db).PlaceOrderAsync(agent,
            new[] { new OrderLine(item, 6), new OrderLine(item, 6) });

        Assert.False(result.Success);
        Assert.Equal(10, test.StockOf(item));
    }

    [Fact]
    public async Task PlaceOrder_UnitAmountIsCopiedFromCurrentItemPrice()
    {
        using var test = new TestDb();
        int agent = test.AddAgent();
        int item = test.AddItem("Oil 1L", 45_500m, 10);

        using var db = test.NewContext();
        await new OrderService(db).PlaceOrderAsync(agent, new[] { new OrderLine(item, 2) });

        using var check = test.NewContext();
        var detail = await check.OrderDetails.AsNoTracking().SingleAsync();
        Assert.Equal(45_500m, detail.UnitAmount);
        Assert.Equal(91_000m, detail.Quantity * detail.UnitAmount);   // line total
    }

    [Fact]
    public async Task PlaceOrder_NoLines_ReturnsErrorWithoutTouchingTheDatabase()
    {
        using var test = new TestDb();
        int agent = test.AddAgent();

        using var db = test.NewContext();
        var result = await new OrderService(db).PlaceOrderAsync(agent, new[] { new OrderLine(1, 0) });

        Assert.False(result.Success);
        Assert.Equal("Add at least one item.", result.Error);
        using var check = test.NewContext();
        Assert.Empty(await check.Orders.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task PlaceOrder_UnknownItem_IsBlockedAndOrderIsRolledBack()
    {
        using var test = new TestDb();
        int agent = test.AddAgent();

        using var db = test.NewContext();
        var result = await new OrderService(db).PlaceOrderAsync(agent, new[] { new OrderLine(9999, 1) });

        Assert.False(result.Success);
        Assert.Equal("Item not found.", result.Error);
        using var check = test.NewContext();
        Assert.Empty(await check.Orders.AsNoTracking().ToListAsync());
    }
}
