namespace Prodea.Api.Models;

public class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string InviteLink { get; set; } = string.Empty;
    public int AdminUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Admin { get; set; } = null!;
    public ICollection<TournamentParticipant> Participants { get; set; } = [];
    public ICollection<MatchdayBadge> MatchdayBadges { get; set; } = [];
    public ICollection<AccumulativeBadge> AccumulativeBadges { get; set; } = [];
}
