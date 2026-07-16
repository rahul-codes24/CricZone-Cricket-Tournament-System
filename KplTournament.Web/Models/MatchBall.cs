using System.ComponentModel.DataAnnotations;

namespace KplTournament.Web.Models
{
    public class MatchBall
    {
        public int Id { get; set; }

        public int MatchId { get; set; }
        public Match? Match { get; set; }

        public int InningsNumber { get; set; } = 1;   // 1 or 2
        public int OverNumber { get; set; }           // 1,2,3...
        public int BallNumber { get; set; }           // 1 to 6 (legal balls)

        [StringLength(100)]
        public string BowlerName { get; set; } = string.Empty;

        [StringLength(100)]
        public string BatsmanName { get; set; } = string.Empty;

        public int Runs { get; set; }                 // batsman runs
        public int Extras { get; set; }               // wides/no-balls etc.
        public bool IsWide { get; set; }
        public bool IsNoBall { get; set; }

        public bool IsWicket { get; set; }
        [StringLength(100)]
        public string? WicketType { get; set; }

        [StringLength(250)]
        public string BallResult { get; set; } = string.Empty; // 1,4,W,Wd,Nb+1 etc.
    }
}