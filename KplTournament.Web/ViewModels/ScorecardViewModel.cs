using KplTournament.Web.Models;

namespace KplTournament.Web.Models.ViewModels
{
    public class ScorecardViewModel
    {
        public Match Match { get; set; } = new Match();
        public InningsScorecardViewModel Innings1 { get; set; } = new InningsScorecardViewModel();
        public InningsScorecardViewModel Innings2 { get; set; } = new InningsScorecardViewModel();
        public List<BallByBallViewModel> Balls { get; set; } = new List<BallByBallViewModel>();
    }

    public class InningsScorecardViewModel
    {
        public int InningsNumber { get; set; }
        public int BattingTeamId { get; set; }
        public string BattingTeamName { get; set; } = string.Empty;
        public string BattingSummary { get; set; } = string.Empty;

        public int TotalRuns { get; set; }
        public int Wickets { get; set; }
        public decimal Overs { get; set; }
        public double RunRate { get; set; }

        public int Byes { get; set; }
        public int LegByes { get; set; }
        public int Wides { get; set; }
        public int NoBalls { get; set; }

        public int Extras => Byes + LegByes + Wides + NoBalls;

        public List<BattingRowViewModel> Batting { get; set; } = new List<BattingRowViewModel>();
        public List<BowlingRowViewModel> Bowling { get; set; } = new List<BowlingRowViewModel>();
        public List<FallOfWicketViewModel> FallOfWickets { get; set; } = new List<FallOfWicketViewModel>();

        /// <summary>
        /// Auto-generated one-line innings highlight, CricHeroes-style,
        /// e.g. "Rahul top-scored with 45 (30) · Sagar took 3/20".
        /// </summary>
        public string Highlight
        {
            get
            {
                var topScorer = Batting.OrderByDescending(b => b.Runs).FirstOrDefault();
                var bestBowler = Bowling
                    .OrderByDescending(b => b.Wickets)
                    .ThenBy(b => b.RunsGiven)
                    .FirstOrDefault(b => b.Wickets > 0);

                var parts = new List<string>();
                if (topScorer != null && topScorer.Runs > 0)
                    parts.Add($"{topScorer.PlayerName} top-scored with {topScorer.Runs} ({topScorer.Balls})");
                if (bestBowler != null)
                    parts.Add($"{bestBowler.PlayerName} took {bestBowler.Wickets}/{bestBowler.RunsGiven}");

                return parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
            }
        }
    }

    public class BattingRowViewModel
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string Status { get; set; } = "not out";
        public int Runs { get; set; }
        public int Balls { get; set; }
        public int Fours { get; set; }
        public int Sixes { get; set; }
        public double StrikeRate { get; set; }
    }

    public class BowlingRowViewModel
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int BallsBowled { get; set; }
        public string OversText { get; set; } = "0.0";
        public int Maidens { get; set; }
        public int RunsGiven { get; set; }
        public int Wickets { get; set; }
        public int NoBalls { get; set; }
        public int Wides { get; set; }
        public double Economy { get; set; }
    }

    public class FallOfWicketViewModel
    {
        public int WicketNumber { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string WicketText { get; set; } = string.Empty;
        public string OverText { get; set; } = string.Empty;
        public int TeamScoreAtFall { get; set; }
    }

    public class BallByBallViewModel
    {
        public int InningsNumber { get; set; }
        public string OverText { get; set; } = string.Empty;
        public string BatsmanName { get; set; } = string.Empty;
        public string BowlerName { get; set; } = string.Empty;
        public string ResultText { get; set; } = string.Empty;
    }
}