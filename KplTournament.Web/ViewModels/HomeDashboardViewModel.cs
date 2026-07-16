using KplTournament.Web.Models;

namespace KplTournament.Web.ViewModels;

public class HomeDashboardViewModel
{
    public List<Team> Teams { get; set; } = new();
    public List<Match> RecentMatches { get; set; } = new();
    public List<PointsRowViewModel> PointsTable { get; set; } = new();
    public List<PlayerStatViewModel> TopBatters { get; set; } = new();
    public List<PlayerStatViewModel> TopBowlers { get; set; } = new();
    public List<PlayerStatViewModel> TopSixHitters { get; set; } = new();
}

public class PointsRowViewModel
{
    public string TeamName { get; set; } = string.Empty;
    public int Played { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
    public int Points { get; set; }
    public decimal NetRunRate { get; set; }
}

public class PlayerStatViewModel
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
