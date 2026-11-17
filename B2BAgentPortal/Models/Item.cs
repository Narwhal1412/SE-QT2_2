using System;
using System.Collections.Generic;

namespace B2BAgentPortal.Models;

public partial class Item
{
    public int ItemId { get; set; }

    public string ItemName { get; set; } = null!;

    public string? Size { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
