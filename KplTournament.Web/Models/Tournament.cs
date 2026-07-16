using System.Collections.Generic;

namespace KplTournament.Web.Models
{
    public class Tournament : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
        public System.DateTime StartDate { get; set; } = System.DateTime.Today;
        public System.DateTime EndDate { get; set; } = System.DateTime.Today;
        public string Status { get; set; } = "Upcoming";

        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}
