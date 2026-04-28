using SportsDashboard.Exceptions;
using SportsDashboard.Processors;
using SportsDashboard.Services;
using SportsDashboard.UI;


using var httpClient = new HttpClient();
IHockeyService hockeyService = new NhlApiService(httpClient);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// repeated network calls within the same session.
IReadOnlyList<SportsDashboard.Models.GameScore>? cachedScores = null;

ConsoleUI.PrintHeader(hockeyService.ProviderName);

while (!cts.IsCancellationRequested)
{
    ConsoleUI.PrintHeader(hockeyService.ProviderName);
    var choice = ConsoleUI.ShowMainMenu();

    try
    {
        switch (choice)
        {
            // 1 - Recent scores
            case 1:
                ConsoleUI.PrintLoading("Fetching recent NHL scores");
                cachedScores = await hockeyService.GetRecentScoresAsync(7, cts.Token);
                ConsoleUI.PrintScores(cachedScores);
                break;

            // 2 — Team stats (requires scores; fetches if not yet cached)
            case 2:
                if (cachedScores is null)
                {
                    ConsoleUI.PrintLoading("Fetching scores to compute stats");
                    cachedScores = await hockeyService.GetRecentScoresAsync(7, cts.Token);
                }
                var teamStats = StatsProcessor.BuildTeamStats(cachedScores);
                ConsoleUI.PrintTeamStats(teamStats);

                var best = StatsProcessor.BestTeam(teamStats);
                var offensive = StatsProcessor.MostOffensiveTeam(teamStats);
                if (best is not null)
                    Console.WriteLine($"\n  🏆 Best win rate : {best.TeamName} ({best.WinRate}%)");
                if (offensive is not null)
                    Console.WriteLine($"  🥅 Most offensive: {offensive.TeamName} ({offensive.AvgGoalsFor:F2} goals/game)");
                break;

            // 3 - Top-scoring games
            case 3:
                if (cachedScores is null)
                {
                    ConsoleUI.PrintLoading("Fetching scores");
                    cachedScores = await hockeyService.GetRecentScoresAsync(7, cts.Token);
                }
                var topGames = StatsProcessor.TopScoringGames(cachedScores, 5);
                ConsoleUI.PrintTopGames(topGames);
                break;

            // 4 - Goal statistics summary
            case 4:
                if (cachedScores is null)
                {
                    ConsoleUI.PrintLoading("Fetching scores");
                    cachedScores = await hockeyService.GetRecentScoresAsync(7, cts.Token);
                }
                var (avg, max, min, total) = StatsProcessor.GoalStats(cachedScores);
                ConsoleUI.PrintGoalStats(avg, max, min, total, cachedScores.Count);
                break;

            // 5 - Playoff series
            case 5:
                ConsoleUI.PrintLoading("Fetching playoff bracket");
                var series = await hockeyService.GetPlayoffSeriesAsync(cts.Token);
                ConsoleUI.PrintPlayoffSeries(series);
                break;

            // 0 — Exit
            case 0:
                Console.WriteLine("\n  Goodbye! 🏒");
                return;

            default:
                ConsoleUI.PrintError("Invalid option. Please enter a number from the menu.");
                await Task.Delay(800, cts.Token);
                continue;
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n  Cancelled.");
        break;
    }
    catch (ApiTimeoutException ex)
    {
        ConsoleUI.PrintError($"Request timed out: {ex.Message}");
    }
    catch (ApiUnavailableException ex)
    {
        ConsoleUI.PrintError($"NHL API is unavailable: {ex.Message}");
    }
    catch (ApiException ex)
    {
        ConsoleUI.PrintError($"API error {ex.StatusCode}: {ex.Message}");
    }
    catch (DataParseException ex)
    {
        ConsoleUI.PrintError($"Failed to parse data: {ex.Message}");
    }
    catch (Exception ex)
    {
        // Unexpected errors — log and continue, don't crash
        ConsoleUI.PrintError($"Unexpected error: {ex.Message}");
    }

    ConsoleUI.PressAnyKey();
}
