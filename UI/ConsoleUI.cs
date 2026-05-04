using SportsDashboard.Models;

namespace SportsDashboard.UI;

// All console rendering is isolated here.
public static class ConsoleUI
{
    // For estetic 
    private const int Width = 62;

    // Menu
    public static void PrintHeader(string providerName)
    {
        Console.Clear();
        PrintLine('=');
        PrintCentered("NHL SPORTS DASHBOARD");
        PrintCentered($"Provider: {providerName}");
        PrintLine('=');
        Console.WriteLine();
    }

    public static int ShowMainMenu()
    {
        Console.WriteLine("  Select an option:");
        Console.WriteLine("  [1] Recent scores (last 7 days)");
        Console.WriteLine("  [2] Team statistics & standings");
        Console.WriteLine("  [3] Top-scoring games");
        Console.WriteLine("  [4] Goal statistics summary");
        Console.WriteLine("  [0] Exit");
        Console.WriteLine();
        Console.Write("  > ");

        var input = Console.ReadLine()?.Trim() ?? "";
        return int.TryParse(input, out var choice) ? choice : -1;
    }

    // Scores
    public static void PrintScores(IReadOnlyList<GameScore> scores)
    {
        PrintSectionHeader("RECENT SCORES");

        if (scores.Count == 0)
        {
            Console.WriteLine("  No games found.");
            return;
        }

        string? lastDate = null;
        foreach (var g in scores)
        {
            var dateStr = g.GameTime.ToString("yyyy-MM-dd");
            if (dateStr != lastDate)
            {
                Console.WriteLine();
                Console.WriteLine($" {dateStr}");
                lastDate = dateStr;
            }

            var result = g.HomeScore > g.AwayScore ? "WIN " : g.HomeScore < g.AwayScore ? "LOSS" : "TIE ";
            Console.WriteLine($"    {g.HomeTeam,4} {g.HomeScore} - {g.AwayScore} {g.AwayTeam,-4}   [{result}]  {g.TotalGoals} goals");
        }
    }

    // Team stats
    public static void PrintTeamStats(IReadOnlyList<TeamStats> stats)
    {
        PrintSectionHeader("TEAM STATISTICS");

        if (stats.Count == 0)
        {
            Console.WriteLine("  No data available.");
            return;
        }

        Console.WriteLine($"  {"Team",-6} {"GP",3} {"W",3} {"L",3} {"GF",4} {"GA",4} {"Diff",5} {"Win%",6} {"AvgGF",6}");
        PrintLine('-');

        foreach (var t in stats)
        {
            var diff = t.GoalDiff >= 0 ? $"+{t.GoalDiff}" : t.GoalDiff.ToString();
            Console.WriteLine(
                $"  {t.TeamName,-6} {t.GamesPlayed,3} {t.Wins,3} {t.Losses,3} " +
                $"{t.GoalsFor,4} {t.GoalsAgainst,4} {diff,5} {t.WinRate,5}% {t.AvgGoalsFor,6:F2}");
        }
    }

    // Top-scoring games
    public static void PrintTopGames(IReadOnlyList<GameScore> games)
    {
        PrintSectionHeader($"TOP {games.Count} HIGHEST-SCORING GAMES");

        for (int i = 0; i < games.Count; i++)
        {
            var g = games[i];
            Console.WriteLine($"  #{i + 1}  {g.HomeTeam} {g.HomeScore} - {g.AwayScore} {g.AwayTeam}  " +
                              $"({g.TotalGoals} goals)  {g.GameTime:yyyy-MM-dd}");
        }
    }

    // Goal stats
    public static void PrintGoalStats(double avg, int max, int min, int total, int gameCount)
    {
        PrintSectionHeader("GOAL STATISTICS SUMMARY");
        Console.WriteLine($"  Games analysed : {gameCount}");
        Console.WriteLine($"  Total goals    : {total}");
        Console.WriteLine($"  Average / game : {avg:F2}");
        Console.WriteLine($"  Max in one game: {max}");
        Console.WriteLine($"  Min in one game: {min}");
    }
    
    // Error / info
    public static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n   {message}");
        Console.ResetColor();
    }

    public static void PrintLoading(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  {message}...");
        Console.ResetColor();
    }

    public static void PressAnyKey()
    {
        Console.WriteLine("\n  Press any key to return to menu...");
        Console.ReadKey(intercept: true);
    }

    // Helpers
    private static void PrintLine(char ch) =>
        Console.WriteLine(new string(ch, Width));

    private static void PrintCentered(string text)
    {
        var pad = Math.Max(0, (Width - text.Length) / 2);
        Console.WriteLine(new string(' ', pad) + text);
    }

    private static void PrintSectionHeader(string title)
    {
        Console.WriteLine();
        PrintLine('-');
        Console.WriteLine($"  {title}");
        PrintLine('-');
    }
}
