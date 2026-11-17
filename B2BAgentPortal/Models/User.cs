using System;
using System.Collections.Generic;

namespace B2BAgentPortal.Models;

public partial class User
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public bool Lock { get; set; }

    public int FailedAttempts { get; set; }

    public string Role { get; set; } = null!;

    public int? AgentId { get; set; }

    public virtual Agent? Agent { get; set; }
}
