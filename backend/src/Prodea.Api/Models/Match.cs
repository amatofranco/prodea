namespace Prodea.Api.Models;

public enum MatchPhase
{
    Group,
    R16,
    QF,
    SF,
    Final
}

public enum MatchStatus
{
    Scheduled,
    InProgress,
    Finished
}

public class Match
{
    public int Id { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public string? HomeTeamFlag { get; set; }
    public string? AwayTeamFlag { get; set; }
    public DateTime MatchDate { get; set; }
    public MatchPhase Phase { get; set; }
    public int? Matchday { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public int? ExternalId { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = [];
}
