/*namespace KplTournament.Web.Models
{
    public class Team : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string PrimaryColor { get; set; } = "#0f172a";
        public string? LogoPath { get; set; }

        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<BattingScore> BattingScores { get; set; } = new List<BattingScore>();
        public ICollection<BowlingScore> BowlingScores { get; set; } = new List<BowlingScore>();
    }
}*/












namespace KplTournament.Web.Models
{
    public class Team : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string PrimaryColor { get; set; } = "#0f172a";
        public string? LogoPath { get; set; }

        public int? TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<BattingScore> BattingScores { get; set; } = new List<BattingScore>();
        public ICollection<BowlingScore> BowlingScores { get; set; } = new List<BowlingScore>();
    }
}