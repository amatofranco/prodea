namespace Prodea.Api.Services;

public class PollingStatusService
{
    public DateTime? LastSuccessfulPoll { get; set; }
    public bool ApiAvailable { get; set; } = true;
}
