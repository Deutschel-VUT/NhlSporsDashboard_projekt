using SportsDashboard.Models;

namespace SportsDashboard.Processors;

public static class StatsProcessor
{
    // Aggregates per-team stats from a list of finished games.
    public static IReadOnlyList<TeamStats> BuildTeamStats(IEnumerable<GameScore> games)
    {
        var dict = new Dictionary<string, (int gp, int w, int l, int gf, int ga)>(StringComparer.OrdinalIgnoreCase);

        foreach (var g in games)
        {
            Update(dict, g.HomeTeam, isWin: g.HomeScore > g.AwayScore, goalsFor: g.HomeScore, goalsAgainst: g.AwayScore);
            Update(dict, g.AwayTeam, isWin: g.AwayScore > g.HomeScore, goalsFor: g.AwayScore, goalsAgainst: g.HomeScore);
        }

        return dict
            .Select(kv => new TeamStats(
                TeamName: kv.Key,
                GamesPlayed: kv.Value.gp,
                Wins: kv.Value.w,
                Losses: kv.Value.l,
                GoalsFor: kv.Value.gf,
                GoalsAgainst: kv.Value.ga))
            .OrderByDescending(t => t.WinRate)
            .ThenByDescending(t => t.GoalDiff)
            .ToList();
    }

    // Returns the N highest-scoring games (most total goals).
    public static IReadOnlyList<GameScore> TopScoringGames(IEnumerable<GameScore> games, int n = 5)
        => games.OrderByDescending(g => g.TotalGoals).Take(n).ToList();
        
    // Computes aggregate goal stats across all games.
    public static (double Avg, int Max, int Min, int Total) GoalStats(IEnumerable<GameScore> games)
    {
        var list = games.Select(g => g.TotalGoals).ToList();
        if (list.Count == 0) return (0, 0, 0, 0);
        return (
            Avg: Math.Round(list.Average(), 2),
            Max: list.Max(),
            Min: list.Min(),
            Total: list.Sum()
        );
    }

    // Finds the team with the best win rate in dataset
    public static TeamStats? BestTeam(IEnumerable<TeamStats> stats)
        => stats.MaxBy(t => t.WinRate);

    // Finds the team with the most goals scored per game.
    public static TeamStats? MostOffensiveTeam(IEnumerable<TeamStats> stats)
        => stats.MaxBy(t => t.AvgGoalsFor);

    private static void Update(
        Dictionary<string, (int gp, int w, int l, int gf, int ga)> dict,
        string team, bool isWin, int goalsFor, int goalsAgainst)
    {
        if (!dict.TryGetValue(team, out var s))
            s = (0, 0, 0, 0, 0);

        dict[team] = (s.gp + 1, s.w + (isWin ? 1 : 0), s.l + (isWin ? 0 : 1),
                      s.gf + goalsFor, s.ga + goalsAgainst);
    }
}
