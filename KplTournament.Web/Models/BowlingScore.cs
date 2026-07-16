

namespace KplTournament.Web.Models
{
    public class BowlingScore : BaseEntity
    {
        public int MatchId { get; set; }
        public Match? Match { get; set; }

        public int PlayerId { get; set; }
        public Player? Player { get; set; }

        public int TeamId { get; set; }
        public Team? Team { get; set; }

        public decimal Overs { get; set; }
        public int RunsGiven { get; set; }
        public int Wickets { get; set; }
        public int Maidens { get; set; }
    }
}
