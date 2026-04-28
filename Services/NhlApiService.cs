using System.Net;
using System.Text.Json;
using SportsDashboard.Exceptions;
using SportsDashboard.Models;

namespace SportsDashboard.Services;

// Fetches NHL data from the official free NHL Stats API (api-web.nhle.com).
// No API key required.
public sealed class NhlApiService : IHockeyService, IDisposable
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public string ProviderName => "NHL Stats API (api-web.nhle.com)";

    public NhlApiService(HttpClient httpClient)
    {
        _http = httpClient;
        _http.BaseAddress = new Uri("https://api-web.nhle.com/v1/");
        _http.Timeout = TimeSpan.FromSeconds(15);
        _http.DefaultRequestHeaders.Add("User-Agent", "SportsDashboard/1.0");
    }

    // Public API
    public async Task<IReadOnlyList<GameScore>> GetRecentScoresAsync(
        int days = 7, CancellationToken ct = default)
    {
        var scores = new List<GameScore>();

        // Fetch scores for each of the past N days concurrently
        var dates = Enumerable.Range(0, days)
            .Select(i => DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd"));

        var tasks = dates.Select(date => FetchScoresForDateAsync(date, ct));

        // Await all in parallel; collect results
        var results = await Task.WhenAll(tasks);
        foreach (var dayScores in results)
            scores.AddRange(dayScores);

        return scores.OrderByDescending(g => g.GameTime).ToList();
    }

    public async Task<IReadOnlyList<PlayoffSeries>> GetPlayoffSeriesAsync(
        CancellationToken ct = default)
    {
        // Playoff bracket for current season
        var json = await GetJsonAsync("playoff-bracket/now", ct);

        return ParsePlayoffSeries(json);
    }

    // Private helpers
    private async Task<List<GameScore>> FetchScoresForDateAsync(string date, CancellationToken ct)
    {
        try
        {
            var json = await GetJsonAsync($"score/{date}", ct);
            return ParseScores(json, date);
        }
        catch (ApiException)
        {
            // A single date failing should not crash the whole request
            return new List<GameScore>();
        }
    }

    private async Task<JsonDocument> GetJsonAsync(string path, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(path, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiTimeoutException($"Request to {path} timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiUnavailableException($"Could not reach NHL API: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new ApiException($"Resource not found: {path}", 404);

            throw new ApiException(
                $"NHL API returned {(int)response.StatusCode} for {path}",
                (int)response.StatusCode);
        }

        try
        {
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            throw new DataParseException($"Failed to parse JSON from {path}", ex);
        }
    }

    private static List<GameScore> ParseScores(JsonDocument doc, string dateStr)
    {
        var scores = new List<GameScore>();

        if (!doc.RootElement.TryGetProperty("games", out var games))
            return scores;

        foreach (var game in games.EnumerateArray())
        {
            try
            {
                var state = game.GetProperty("gameState").GetString() ?? "";
                // Only include finished or live games
                if (state is not ("OFF" or "FINAL" or "LIVE" or "CRIT"))
                    continue;

                var home = game.GetProperty("homeTeam");
                var away = game.GetProperty("awayTeam");

                var homeAbbr = home.GetProperty("abbrev").GetString() ?? "???";
                var awayAbbr = away.GetProperty("abbrev").GetString() ?? "???";
                var homeScore = home.TryGetProperty("score", out var hs) ? hs.GetInt32() : 0;
                var awayScore = away.TryGetProperty("score", out var as_) ? as_.GetInt32() : 0;

                var gameTime = DateTime.TryParse(dateStr, out var dt) ? dt : DateTime.UtcNow;

                scores.Add(new GameScore(homeAbbr, awayAbbr, homeScore, awayScore, state, gameTime));
            }
            catch (Exception)
            {
            
            }
        }

        return scores;
    }

    private static List<PlayoffSeries> ParsePlayoffSeries(JsonDocument doc)
    {
        var series = new List<PlayoffSeries>();

        if (!doc.RootElement.TryGetProperty("rounds", out var rounds))
            return series;

        foreach (var round in rounds.EnumerateArray())
        {
            if (!round.TryGetProperty("series", out var seriesList)) continue;

            foreach (var s in seriesList.EnumerateArray())
            {
                try
                {
                    var teams = s.GetProperty("matchupTeams");
                    if (teams.GetArrayLength() < 2) continue;

                    var t1 = teams[0];
                    var t2 = teams[1];

                    var team1 = t1.GetProperty("abbrev").GetString() ?? "???";
                    var team2 = t2.GetProperty("abbrev").GetString() ?? "???";
                    var wins1 = t1.TryGetProperty("wins", out var w1) ? w1.GetInt32() : 0;
                    var wins2 = t2.TryGetProperty("wins", out var w2) ? w2.GetInt32() : 0;

                    var status = s.TryGetProperty("seriesStatus", out var st)
                        ? st.GetString() ?? "unknown"
                        : "unknown";

                    series.Add(new PlayoffSeries(team1, team2, wins1, wins2, status));
                }
                catch
                {
                 
                }
            }
        }

        return series;
    }

    public void Dispose() => _http.Dispose();
}
