using System;
using System.Collections.Generic;

namespace MazeBall.Database.Entities;

public partial class MatchResult
{
    public string Username { get; set; } = null!;

    public int MatchId { get; set; }

    public bool Result { get; set; }

    public virtual Match Match { get; set; } = null!;

    public virtual User UsernameNavigation { get; set; } = null!;
}
