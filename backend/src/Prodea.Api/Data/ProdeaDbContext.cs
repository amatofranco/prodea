using Microsoft.EntityFrameworkCore;
using Prodea.Api.Models;

namespace Prodea.Api.Data;

public class ProdeaDbContext(DbContextOptions<ProdeaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<MatchdayBadge> MatchdayBadges => Set<MatchdayBadge>();
    public DbSet<AccumulativeBadge> AccumulativeBadges => Set<AccumulativeBadge>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<PredictionBackup> PredictionBackups => Set<PredictionBackup>();
    public DbSet<ChampionPick> ChampionPicks => Set<ChampionPick>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.GoogleId).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.Email).HasMaxLength(200);
        });

        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasIndex(t => t.Token).IsUnique();
            e.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tournament>(e =>
        {
            e.HasIndex(t => t.Code).IsUnique();
            e.HasIndex(t => t.InviteLink).IsUnique();
            e.Property(t => t.Code).HasMaxLength(6);
            e.Property(t => t.Name).HasMaxLength(100);
            e.HasOne(t => t.Admin)
                .WithMany(u => u.AdminTournaments)
                .HasForeignKey(t => t.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TournamentParticipant>(e =>
        {
            e.HasIndex(tp => new { tp.TournamentId, tp.UserId }).IsUnique();
            e.HasOne(tp => tp.Tournament)
                .WithMany(t => t.Participants)
                .HasForeignKey(tp => tp.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(tp => tp.User)
                .WithMany(u => u.TournamentParticipants)
                .HasForeignKey(tp => tp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Match>(e =>
        {
            e.HasIndex(m => m.ExternalId);
            e.Property(m => m.HomeTeam).HasMaxLength(100);
            e.Property(m => m.AwayTeam).HasMaxLength(100);
            e.Property(m => m.Status).HasConversion<string>();
            e.Property(m => m.Phase).HasConversion<string>();
        });

        modelBuilder.Entity<Prediction>(e =>
        {
            e.HasIndex(p => new { p.UserId, p.MatchId }).IsUnique();
            e.HasOne(p => p.User)
                .WithMany(u => u.Predictions)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Match)
                .WithMany(m => m.Predictions)
                .HasForeignKey(p => p.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MatchdayBadge>(e =>
        {
            e.HasIndex(mb => new { mb.UserId, mb.TournamentId, mb.Phase, mb.Matchday }).IsUnique();
            e.Property(mb => mb.BadgeType).HasConversion<string>();
            e.HasOne(mb => mb.User)
                .WithMany(u => u.MatchdayBadges)
                .HasForeignKey(mb => mb.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(mb => mb.Tournament)
                .WithMany(t => t.MatchdayBadges)
                .HasForeignKey(mb => mb.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PredictionBackup>(e =>
        {
            e.Property(b => b.JsonData).HasColumnType("text");
        });

        modelBuilder.Entity<AccumulativeBadge>(e =>
        {
            e.HasIndex(ab => new { ab.UserId, ab.TournamentId, ab.BadgeType }).IsUnique();
            e.Property(ab => ab.BadgeType).HasConversion<string>();
            e.HasOne(ab => ab.User)
                .WithMany(u => u.AccumulativeBadges)
                .HasForeignKey(ab => ab.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ab => ab.Tournament)
                .WithMany(t => t.AccumulativeBadges)
                .HasForeignKey(ab => ab.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChampionPick>(e =>
        {
            e.HasIndex(cp => new { cp.TournamentId, cp.UserId }).IsUnique();
            e.Property(cp => cp.CountryName).HasMaxLength(100);
            e.HasOne(cp => cp.User)
                .WithMany(u => u.ChampionPicks)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cp => cp.Tournament)
                .WithMany(t => t.ChampionPicks)
                .HasForeignKey(cp => cp.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
