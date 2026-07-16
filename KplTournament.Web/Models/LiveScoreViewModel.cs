namespace KplTournament.Web.Models
{
    public class LiveScoreViewModel
    {
        public int MatchId { get; set; }
        public LiveScore LiveScore { get; set; }
        public Match Match { get; set; }
    }
}