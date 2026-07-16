using KplTournament.Web.Models;

namespace KplTournament.Web.ViewModels
{
    public class LiveScoringViewModel
    {
        public Match Match { get; set; } = new Match();
        public List<MatchBall> Balls { get; set; } = new List<MatchBall>();

        public string NewBowlerName { get; set; } = string.Empty;
        public string BatsmanName { get; set; } = string.Empty;

        public int Runs { get; set; }
        public int Extras { get; set; }
        public bool IsWide { get; set; }
        public bool IsNoBall { get; set; }
        public bool IsWicket { get; set; }
        public string? WicketType { get; set; }
    }
}