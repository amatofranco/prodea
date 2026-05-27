namespace Prodea.Api.Models;

public class PredictionBackup
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Count { get; set; }
    public string JsonData { get; set; } = string.Empty;
}
