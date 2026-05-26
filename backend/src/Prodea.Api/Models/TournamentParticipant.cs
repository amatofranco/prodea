namespace Prodea.Api.Models;

public class TournamentParticipant
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Tournament Tournament { get; set; } = null!;
    public User User { get; set; } = null!;
}
