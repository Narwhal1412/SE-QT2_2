using System;
using System.Collections.Generic;

namespace B2BAgentPortal.Models;

public partial class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitAmount { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
