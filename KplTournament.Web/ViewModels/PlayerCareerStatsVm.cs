namespace KplTournament.Web.ViewModels
{
    public class PlayerCareerStatsVm
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = "";
        public string TeamName { get; set; } = "";
        public string? PhotoPath { get; set; }

        // Batting career totals
        public int Matches { get; set; }
        public int Innings { get; set; }
        public int Runs { get; set; }
        public int BallsFaced { get; set; }
        public int Fours { get; set; }
        public int Sixes { get; set; }
        public int Dismissals { get; set; }
        public int HighestScore { get; set; }
        public int Fifties { get; set; }
        public int Hundreds { get; set; }

        public double BattingAverage => Dismissals > 0 ? Math.Round((double)Runs / Dismissals, 2) : Runs;
        public double BattingStrikeRate => BallsFaced > 0 ? Math.Round((Runs * 100.0) / BallsFaced, 2) : 0;

        // Bowling career totals
        public int WicketsTaken { get; set; }
        public int RunsConceded { get; set; }
        public int BallsBowled { get; set; }
        public string BestBowling { get; set; } = "-";

        public double BowlingEconomy => BallsBowled > 0 ? Math.Round((RunsConceded * 6.0) / BallsBowled, 2) : 0;
        public double BowlingAverage => WicketsTaken > 0 ? Math.Round((double)RunsConceded / WicketsTaken, 2) : 0;
        public string OversBowledText => $"{BallsBowled / 6}.{BallsBowled % 6}";

        // Fielding
        public int Catches { get; set; }
        public int RunOuts { get; set; }
    }
}
