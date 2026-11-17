using B2BAgentPortal.Data;
using B2BAgentPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace B2BAgentPortal.Services;

public record OrderLine(int ItemId, int Quantity);
public record PlaceOrderResult(bool Success, int? OrderId, string? Error);

public class OrderService
{
    private readonly B2BContext _db;
    public OrderService(B2BContext db) => _db = db;

    // Pure logic, no database: drop empty lines and merge duplicate items.
    // Needed because the table has UNIQUE(OrderID, ItemID). Easy to unit-test.
    public static List<OrderLine> MergeLines(IEnumerable<OrderLine> lines) =>
        lines.Where(l => l.Quantity > 0)
             .GroupBy(l => l.ItemId)
             .Select(g => new OrderLine(g.Key, g.Sum(l => l.Quantity)))
             .ToList();

    public async Task<PlaceOrderResult> PlaceOrderAsync(int agentId, IEnumerable<OrderLine> lines)
    {
        var merged = MergeLines(lines);
        if (merged.Count == 0) return new(false, null, "Add at least one item.");

        // Everything below succeeds together or not at all
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Create the order (master)
            var order = new Order { AgentId = agentId, OrderDate = DateTime.Now, OrderStatus = "Pending" };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();                       // gives us order.OrderId

            foreach (var line in merged)
            {
                var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemId == line.ItemId);
                if (item == null) { await tx.RollbackAsync(); return new(false, null, "Item not found."); }

                // 2. Deduct stock. The WHERE makes the check and the deduction one
                //    atomic step, so two agents cannot both take the last units.
                int rows = await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE Item SET StockQuantity = StockQuantity - {line.Quantity} WHERE ItemID = {line.ItemId} AND StockQuantity >= {line.Quantity}");

                if (rows == 0)   // nothing updated = not enough stock
                {
                    await tx.RollbackAsync();                   // also undoes the order and earlier lines
                    return new(false, null, $"Insufficient stock for {item.ItemName}.");
                }

                // 3. Add the detail line (UnitAmount copied from the price now)
                _db.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId, ItemId = line.ItemId,
                    Quantity = line.Quantity, UnitAmount = item.Price
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();                             // only now is everything saved
            return new(true, order.OrderId, null);
        }
        catch
        {
            await tx.RollbackAsync();                           // any unexpected error undoes everything
            throw;
        }
    }
}