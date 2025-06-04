using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MazeBall.Database.Entities;

public partial class MazeBallContext : DbContext
{
    public MazeBallContext()
    {
    }

    public MazeBallContext(DbContextOptions<MazeBallContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Match> Matches { get; set; }

    public virtual DbSet<MatchResult> MatchResults { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("name=DatabaseConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("Match");

            entity.Property(e => e.MatchId).HasColumnName("MatchID");
            entity.Property(e => e.CreationDate).HasColumnType("date");
            entity.Property(e => e.RoomName).HasMaxLength(50);
        });

        modelBuilder.Entity<MatchResult>(entity =>
        {
            entity.HasKey(e => new { e.Username, e.MatchId });

            entity.ToTable("MatchResult");

            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.MatchId).HasColumnName("MatchID");

            entity.HasOne(d => d.Match).WithMany(p => p.MatchResults)
                .HasForeignKey(d => d.MatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MatchResult_Match");

            entity.HasOne(d => d.UsernameNavigation).WithMany(p => p.MatchResults)
                .HasForeignKey(d => d.Username)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MatchResult_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Username);

            entity.ToTable("User");

            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.Active)
                .IsRequired()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.BirthDate).HasColumnType("date");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Password).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
