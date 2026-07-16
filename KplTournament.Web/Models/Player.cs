/*namespace KplTournament.Web.Models;

public class Player : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsCaptain { get; set; }
    public bool IsViceCaptain { get; set; }
    public bool IsInjured { get; set; }
    public int TeamId { get; set; }
    public Team? Team { get; set; }
    
}*/

namespace KplTournament.Web.Models
{
    public class Player : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsCaptain { get; set; }
        public bool IsViceCaptain { get; set; }
        public bool IsInjured { get; set; }

        public string? PhotoPath { get; set; }

        // Used only for binding the radio-button choice in EditTeamPlayers.
        // Not persisted — the controller maps it back onto the three bool flags above.
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Status { get; set; } = "None";

        public int TeamId { get; set; }
        public Team? Team { get; set; }

        public ICollection<BattingScore> BattingScores { get; set; } = new List<BattingScore>();
        public ICollection<BowlingScore> BowlingScores { get; set; } = new List<BowlingScore>();
    }
}
