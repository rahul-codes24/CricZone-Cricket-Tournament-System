namespace KplTournament.Web.Models
{
    public class FallOfWicket : BaseEntity
    {
        public int MatchId { get; set; }
        public Match? Match { get; set; }

        public int Innings { get; set; }   // 1 or 2

        public int TeamId { get; set; }
        public Team? Team { get; set; }

        public int PlayerId { get; set; }
        public Player? Player { get; set; }

        public int WicketNumber { get; set; }   // 1st wicket, 2nd wicket...

        public int Score { get; set; }          // team score when wicket fell

        public int OverNumber { get; set; }     // 0,1,2,3...
        public int BallNumber { get; set; }     // 1 to 6

        public string DisplayText
        {
            get
            {
                return $"{WicketNumber}-{Score} ({Player?.Name ?? "Player"}, {OverNumber}.{BallNumber})";
            }
        }
    }
}