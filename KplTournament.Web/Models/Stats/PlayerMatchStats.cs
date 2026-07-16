namespace KplTournament.Web.Models.Stats
{
    public class PlayerMatchStats
    {
        public string PlayerName { get; set; } = "";

        // Batting
        public int Runs { get; set; } = 0;
        public int Balls { get; set; } = 0;
        public int Fours { get; set; } = 0;
        public int Sixes { get; set; } = 0;

        // Bowling
        public int Wickets { get; set; } = 0;
        public int RunsGiven { get; set; } = 0;
        public int BallsBowled { get; set; } = 0;

        // Extra (optional future use)
        public int Catches { get; set; } = 0;
        public int RunOuts { get; set; } = 0;

        // ✅ PERFORMANCE SCORE (for Man of the Match)
        public double PerformanceScore
        {
            get
            {
                return (Runs * 1.0)
                     + (Wickets * 25.0)
                     + (Catches * 10.0);
            }
        }
    }
}