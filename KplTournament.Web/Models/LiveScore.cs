using System;

namespace KplTournament.Web.Models
{
    public class LiveScore
    {
        public int Id { get; set; }
        public int MatchId { get; set; }

        public int TotalRuns { get; set; }
        public int Wickets { get; set; }

        public int Overs { get; set; }
        public int Balls { get; set; }

        public int CurrentInnings { get; set; } = 1;

        public string StrikerName { get; set; }
        public string NonStrikerName { get; set; }
        public string BowlerName { get; set; }

        public int StrikerRuns { get; set; }
        public int StrikerBalls { get; set; }

        public int NonStrikerRuns { get; set; }
        public int NonStrikerBalls { get; set; }

        public int BowlerRunsGiven { get; set; }
        public int BowlerWickets { get; set; }
        public int BowlerBalls { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}