using System;
using System.Collections.Generic;

namespace B2BAgentPortal.Models;

public partial class Agent
{
    public int AgentId { get; set; }

    public string AgentName { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
