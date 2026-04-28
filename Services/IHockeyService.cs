using SportsDashboard.Models;

namespace SportsDashboard.Services;

// Defines contract for fetching NHL data from an external source.
// Allows easy swapping of providers (e.g. NHL API, ESPN, mock).
public interface IHockeyService
{
    Task<IReadOnlyList<GameScore>> GetRecentScoresAsync(int days = 7, CancellationToken ct = default);
    Task<IReadOnlyList<PlayoffSeries>> GetPlayoffSeriesAsync(CancellationToken ct = default);
    string ProviderName { get; }
}
