/*
namespace KplTournament.Web.Models
{
    public class Match : BaseEntity
    {
        public DateTime MatchDate { get; set; } = DateTime.Today;
        public string Venue { get; set; } = string.Empty;
        public int OversPerInnings { get; set; } = 10;

        public int TeamAId { get; set; }
        public Team? TeamA { get; set; }

        public int TeamBId { get; set; }
        public Team? TeamB { get; set; }

        public string TossWinner { get; set; } = string.Empty;
        public string TossDecision { get; set; } = string.Empty;

        public int TeamAScore { get; set; }
        public int TeamAWickets { get; set; }
        public decimal TeamAOvers { get; set; }

        public int TeamBScore { get; set; }
        public int TeamBWickets { get; set; }
        public decimal TeamBOvers { get; set; }

        public string Result { get; set; } = string.Empty;
        public string? ResultText { get; set; }

        public int? WinnerTeamId { get; set; }
        public Team? WinnerTeam { get; set; }

        // ✅ Man of the Match (Correct Setup)
        public int? ManOfTheMatchPlayerId { get; set; }
        public Player? ManOfTheMatchPlayer { get; set; }

        public int? TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        public string Status { get; set; } = "Upcoming";

        // 1 = First innings, 2 = Second innings
        public int CurrentInnings { get; set; } = 1;

        // Current live total
        public int CurrentRuns { get; set; } = 0;
        public int CurrentWickets { get; set; } = 0;

        // Over format fix (IMPORTANT)
        public int CurrentOver { get; set; } = 0;
        public int CurrentBall { get; set; } = 0;

        // Extras
        public int CurrentWideRuns { get; set; } = 0;
        public int CurrentNoBallRuns { get; set; } = 0;
        public int CurrentLegByeRuns { get; set; } = 0;

        // Current players on field
        public int? StrikerId { get; set; }
        public Player? Striker { get; set; }

        public int? NonStrikerId { get; set; }
        public Player? NonStriker { get; set; }

        public int? CurrentBowlerId { get; set; }
        public Player? CurrentBowler { get; set; }

        // Target for second innings
        public int? Target { get; set; }

        // Over / innings control
        public bool IsOverCompleted { get; set; } = false;
        public bool IsInningsCompleted { get; set; } = false;

        // ✅ Add this (helps to trigger MOM logic safely)
        public bool IsMatchCompleted { get; set; } = false;

        public ICollection<BattingScore> BattingScores { get; set; } = new List<BattingScore>();
        public ICollection<BowlingScore> BowlingScores { get; set; } = new List<BowlingScore>();
        public ICollection<BallByBall> BallByBalls { get; set; } = new List<BallByBall>();
    }
}*/











namespace KplTournament.Web.Models
{
    public class Match : BaseEntity
    {
        public DateTime MatchDate { get; set; } = DateTime.Today;
        public string Venue { get; set; } = string.Empty;
        public int OversPerInnings { get; set; } = 10;

        // Teams
        public int TeamAId { get; set; }
        public Team? TeamA { get; set; }

        public int TeamBId { get; set; }
        public Team? TeamB { get; set; }

        // Toss
        public string TossWinner { get; set; } = string.Empty;
        public string TossDecision { get; set; } = string.Empty;

        // Scores (Final / Stored)
        public int TeamAScore { get; set; }
        public int TeamAWickets { get; set; }
        public decimal TeamAOvers { get; set; }

        public int TeamBScore { get; set; }
        public int TeamBWickets { get; set; }
        public decimal TeamBOvers { get; set; }

        // Result
        public string Result { get; set; } = string.Empty;
        public string? ResultText { get; set; }

        public int? WinnerTeamId { get; set; }
        public Team? WinnerTeam { get; set; }

        // Man of the Match
        public int? ManOfTheMatchPlayerId { get; set; }
        public Player? ManOfTheMatchPlayer { get; set; }

        public int? TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        // Status: Upcoming / Live / Completed / Innings Break
        public string Status { get; set; } = "Upcoming";

        // MatchType: League Match / Quarter Final Match / Semi Final Match / Final Match / Other Match
        public string MatchType { get; set; } = "League Match";

        // Live Match Tracking
        public int CurrentInnings { get; set; } = 1;

        public int CurrentRuns { get; set; } = 0;
        public int CurrentWickets { get; set; } = 0;

        public int CurrentOver { get; set; } = 0;
        public int CurrentBall { get; set; } = 0;

        // Extras
        public int CurrentWideRuns { get; set; } = 0;
        public int CurrentNoBallRuns { get; set; } = 0;
        public int CurrentLegByeRuns { get; set; } = 0;

        // Players on field
        public int? StrikerId { get; set; }
        public Player? Striker { get; set; }

        public int? NonStrikerId { get; set; }
        public Player? NonStriker { get; set; }

        public int? CurrentBowlerId { get; set; }
        public Player? CurrentBowler { get; set; }

        // Target
        public int? Target { get; set; }

        // Control Flags
        public bool IsOverCompleted { get; set; } = false;
        public bool IsInningsCompleted { get; set; } = false;
        public bool IsMatchCompleted { get; set; } = false;

        // Snapshot of team runs / legal balls at the moment the current batting
        // pair came to the crease. Used to derive the live partnership below.
        public int PartnershipRunsStart { get; set; } = 0;
        public int PartnershipBallsStart { get; set; } = 0;

        // ---- Derived, CricHeroes-style live stats (not stored in DB) ----

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int LegalBallsBowled => (CurrentOver * 6) + CurrentBall;

        /// <summary>Current Run Rate: runs per over so far in this innings.</summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public decimal CurrentRunRate =>
            LegalBallsBowled > 0 ? Math.Round((CurrentRuns * 6m) / LegalBallsBowled, 2) : 0m;

        /// <summary>Required Run Rate: only meaningful during the second innings while chasing.</summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public decimal? RequiredRunRate
        {
            get
            {
                if (CurrentInnings != 2 || !Target.HasValue) return null;

                int ballsLeft = (OversPerInnings * 6) - LegalBallsBowled;
                int runsNeeded = Target.Value - CurrentRuns;

                if (ballsLeft <= 0 || runsNeeded <= 0) return null;

                return Math.Round((runsNeeded * 6m) / ballsLeft, 2);
            }
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int PartnershipRuns => Math.Max(0, CurrentRuns - PartnershipRunsStart);

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int PartnershipBalls => Math.Max(0, LegalBallsBowled - PartnershipBallsStart);

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string PartnershipText => $"{PartnershipRuns} ({PartnershipBalls} balls)";

        // Navigation
        public ICollection<BattingScore> BattingScores { get; set; } = new List<BattingScore>();
        public ICollection<BowlingScore> BowlingScores { get; set; } = new List<BowlingScore>();
        public ICollection<BallByBall> BallByBalls { get; set; } = new List<BallByBall>();
    }
}