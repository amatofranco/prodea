namespace Prodea.Api.Models;

public enum AccumulativeBadgeType
{
    EnCaidaLibre,
    RachaInfernal,
    ElMuro,
    ElFantasma
}

public class AccumulativeBadge
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TournamentId { get; set; }
    public AccumulativeBadgeType BadgeType { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Tournament Tournament { get; set; } = null!;
}
