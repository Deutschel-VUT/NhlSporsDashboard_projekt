namespace SportsDashboard.Models;

public record GameScore(
    string HomeTeam,
    string AwayTeam,
    int HomeScore,
    int AwayScore,
    string Status,
    DateTime GameTime
)
{
    public string Winner => HomeScore > AwayScore ? HomeTeam : AwayScore > HomeScore ? AwayTeam : "TIE";
    public int TotalGoals => HomeScore + AwayScore;
}

public record TeamStats(
    string TeamName,
    int GamesPlayed,
    int Wins,
    int Losses,
    int GoalsFor,
    int GoalsAgainst
)
{
    public double WinRate => GamesPlayed == 0 ? 0 : Math.Round((double)Wins / GamesPlayed * 100, 1);
    public int GoalDiff => GoalsFor - GoalsAgainst;
    public double AvgGoalsFor => GamesPlayed == 0 ? 0 : Math.Round((double)GoalsFor / GamesPlayed, 2);
    public double AvgGoalsAgainst => GamesPlayed == 0 ? 0 : Math.Round((double)GoalsAgainst / GamesPlayed, 2);
}

public record PlayoffSeries(
    string HomeTeam,
    string AwayTeam,
    int HomeWins,
    int AwayWins,
    string Status
)
{
    public string Leader => HomeWins > AwayWins ? HomeTeam : AwayWins > HomeWins ? AwayTeam : "Tied";
    public string SeriesScore => $"{HomeTeam} {HomeWins}-{AwayWins} {AwayTeam}";
}
