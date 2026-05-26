namespace Prodea.Api.Models;

public enum MatchdayBadgeType
{
    Crack,
    Mufa,
    Adivino,
    Francotirador,
    Payaso,
    Dormido
}

public class MatchdayBadge
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TournamentId { get; set; }
    public int Matchday { get; set; }
    public MatchdayBadgeType BadgeType { get; set; }
    public int PointsInMatchday { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Tournament Tournament { get; set; } = null!;
}
