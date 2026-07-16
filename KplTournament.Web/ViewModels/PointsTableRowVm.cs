namespace KplTournament.Web.ViewModels
{
    public class PointsTableRowVm
    {
        public string TeamName { get; set; } = "";
        public int Played { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public int Points { get; set; }
        public decimal NetRunRate { get; set; }
    }
}