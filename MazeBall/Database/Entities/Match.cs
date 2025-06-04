using System;
using System.Collections.Generic;

namespace MazeBall.Database.Entities;

public partial class Match
{
    public int MatchId { get; set; }

    public DateTime CreationDate { get; set; }

    public string RoomName { get; set; } = null!;

    public virtual ICollection<MatchResult> MatchResults { get; set; } = new List<MatchResult>();
}
