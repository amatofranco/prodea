namespace Prodea.Api.Models;

public class ChampionPick
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;
    public string CountryName { get; set; } = "";
    public DateTime PickedAt { get; set; } = DateTime.UtcNow;
    public int PointsEarned { get; set; }
}
