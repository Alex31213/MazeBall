using System;
using System.Collections.Generic;

namespace MazeBall.Database.Entities;

public partial class User
{
    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime BirthDate { get; set; }

    public bool? Active { get; set; }

    public virtual ICollection<MatchResult> MatchResults { get; set; } = new List<MatchResult>();
}
