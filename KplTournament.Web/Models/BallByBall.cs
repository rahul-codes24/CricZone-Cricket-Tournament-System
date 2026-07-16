/*namespace KplTournament.Web.Models
{
    public class BallByBall : BaseEntity
    {
        public int MatchId { get; set; }
        public Match? Match { get; set; }

        // 1 = first innings, 2 = second innings
        public int InningsNumber { get; set; } = 1;

        // Example: Over 1 Ball 3
        public int OverNumber { get; set; }
        public int BallNumber { get; set; }

        // Bat runs
        public int Runs { get; set; } = 0;

        // Extra runs
        public int Extras { get; set; } = 0;

        public bool IsWicket { get; set; } = false;
        public bool IsWide { get; set; } = false;
        public bool IsNoBall { get; set; } = false;
        public bool IsBye { get; set; } = false;
        public bool IsLegBye { get; set; } = false;

        // True only for legal delivery
        public bool IsLegalBall { get; set; } = true;

        public string? WicketType { get; set; }

        public int? BatsmanId { get; set; }
        public Player? Batsman { get; set; }

        public int? BowlerId { get; set; }
        public Player? Bowler { get; set; }

        public string BallText { get; set; } = string.Empty;
        // Examples: "0", "1", "4", "6", "W", "Wd", "Nb", "LB1", "B1"
    }
}*/








using System.ComponentModel.DataAnnotations.Schema;

namespace KplTournament.Web.Models
{
    public class BallByBall : BaseEntity
    {
        public int MatchId { get; set; }
        public Match? Match { get; set; }

        // Current innings number
        public int InningsNumber { get; set; } = 1;

        public int OverNumber { get; set; }
        public int BallNumber { get; set; }

        // Runs from bat
        public int Runs { get; set; } = 0;

        // Extras on this ball
        public int Extras { get; set; } = 0;

        public bool IsWicket { get; set; } = false;
        public bool IsWide { get; set; } = false;
        public bool IsNoBall { get; set; } = false;
        public bool IsBye { get; set; } = false;
        public bool IsLegBye { get; set; } = false;

        // Legal delivery or not
        public bool IsLegalBall { get; set; } = true;

        // Striker batsman on this ball
        public int? BatsmanId { get; set; }
        public Player? Batsman { get; set; }

        // Bowler of this ball
        public int? BowlerId { get; set; }
        public Player? Bowler { get; set; }

        // Simple display text for this ball
        // Example: 1 / 4 / W / Wd / Nb+2 / Rahul c Amit b Sagar
        public string BallText { get; set; } = string.Empty;

        // Wicket details
        public string? WicketType { get; set; }

        public int? OutBatsmanId { get; set; }
        public Player? OutBatsman { get; set; }

        public int? FielderId { get; set; }
        public Player? Fielder { get; set; }
    }
}