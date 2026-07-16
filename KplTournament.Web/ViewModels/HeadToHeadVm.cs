using KplTournament.Web.Models;

namespace KplTournament.Web.ViewModels
{
    public class HeadToHeadVm
    {
        public Team TeamA { get; set; } = null!;
        public Team TeamB { get; set; } = null!;

        public int MatchesPlayed { get; set; }
        public int TeamAWins { get; set; }
        public int TeamBWins { get; set; }
        public int NoResult { get; set; }

        public int TeamAHighestScore { get; set; }
        public int TeamBHighestScore { get; set; }

        public List<Match> Matches { get; set; } = new();
    }
}
