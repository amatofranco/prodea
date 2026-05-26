namespace Prodea.Api.Models;

public class Prediction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TournamentId { get; set; }
    public int MatchId { get; set; }
    public int PredictedHomeScore { get; set; }
    public int PredictedAwayScore { get; set; }
    public int PointsEarned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Tournament Tournament { get; set; } = null!;
    public Match Match { get; set; } = null!;
}
