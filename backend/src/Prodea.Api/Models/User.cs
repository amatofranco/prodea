namespace Prodea.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = [];
    public ICollection<Tournament> AdminTournaments { get; set; } = [];
    public ICollection<Prediction> Predictions { get; set; } = [];
    public ICollection<MatchdayBadge> MatchdayBadges { get; set; } = [];
    public ICollection<AccumulativeBadge> AccumulativeBadges { get; set; } = [];
    public ICollection<ChampionPick> ChampionPicks { get; set; } = [];
}
