using System;
using System.Collections.Generic;

namespace MazeBall.Database.Entities;

public partial class Result
{
    public string Username { get; set; } = null!;

    public int Id { get; set; }

    public bool Result1 { get; set; }
}
