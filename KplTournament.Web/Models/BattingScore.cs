/*namespace KplTournament.Web.Models;

public class BattingScore : BaseEntity
{
    public int MatchId { get; set; }
    public Match? Match { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    public int TeamId { get; set; }
    public Team? Team { get; set; }
    public int Runs { get; set; }
    public int Balls { get; set; }
    public int Fours { get; set; }
    public int Sixes { get; set; }
    public bool IsOut { get; set; }
}*/

namespace KplTournament.Web.Models
{
    public class BattingScore : BaseEntity
    {
        public int MatchId { get; set; }
        public Match? Match { get; set; }

        public int PlayerId { get; set; }
        public Player? Player { get; set; }

        public int TeamId { get; set; }
        public Team? Team { get; set; }

        public int Runs { get; set; }
        public int Balls { get; set; }
        public int Fours { get; set; }
        public int Sixes { get; set; }
        public bool IsOut { get; set; }
    }
}
