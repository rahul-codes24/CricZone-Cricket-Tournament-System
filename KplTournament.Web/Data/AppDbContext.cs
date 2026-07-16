



using KplTournament.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace KplTournament.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Tournament> Tournaments => Set<Tournament>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<BattingScore> BattingScores => Set<BattingScore>();
        public DbSet<BowlingScore> BowlingScores => Set<BowlingScore>();
        public DbSet<BallByBall> BallByBalls => Set<BallByBall>();
        public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
        public DbSet<LiveScore> LiveScores { get; set; }
        public DbSet<MatchBall> MatchBalls { get; set; }
        public DbSet<FallOfWicket> FallOfWickets { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Team -> Players
            modelBuilder.Entity<Player>()
                .HasOne(p => p.Team)
                .WithMany(t => t.Players)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match -> TeamA
            modelBuilder.Entity<Match>()
                .HasOne(m => m.TeamA)
                .WithMany()
                .HasForeignKey(m => m.TeamAId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match -> TeamB
            modelBuilder.Entity<Match>()
                .HasOne(m => m.TeamB)
                .WithMany()
                .HasForeignKey(m => m.TeamBId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match -> WinnerTeam
            modelBuilder.Entity<Match>()
                .HasOne(m => m.WinnerTeam)
                .WithMany()
                .HasForeignKey(m => m.WinnerTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match -> ManOfTheMatchPlayer
            modelBuilder.Entity<Match>()
                .HasOne(m => m.ManOfTheMatchPlayer)
                .WithMany()
                .HasForeignKey(m => m.ManOfTheMatchPlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match -> Striker
            modelBuilder.Entity<Match>()
                .HasOne(m => m.Striker)
                .WithMany()
                .HasForeignKey(m => m.StrikerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match -> NonStriker
            modelBuilder.Entity<Match>()
                .HasOne(m => m.NonStriker)
                .WithMany()
                .HasForeignKey(m => m.NonStrikerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match -> CurrentBowler
            modelBuilder.Entity<Match>()
                .HasOne(m => m.CurrentBowler)
                .WithMany()
                .HasForeignKey(m => m.CurrentBowlerId)
                .OnDelete(DeleteBehavior.Restrict);

            // BattingScore -> Match
            modelBuilder.Entity<BattingScore>()
                .HasOne(bs => bs.Match)
                .WithMany(m => m.BattingScores)
                .HasForeignKey(bs => bs.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // BattingScore -> Player
            modelBuilder.Entity<BattingScore>()
                .HasOne(bs => bs.Player)
                .WithMany(p => p.BattingScores)
                .HasForeignKey(bs => bs.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // BattingScore -> Team
            modelBuilder.Entity<BattingScore>()
                .HasOne(bs => bs.Team)
                .WithMany(t => t.BattingScores)
                .HasForeignKey(bs => bs.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // BowlingScore -> Match
            modelBuilder.Entity<BowlingScore>()
                .HasOne(bs => bs.Match)
                .WithMany(m => m.BowlingScores)
                .HasForeignKey(bs => bs.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // BowlingScore -> Player
            modelBuilder.Entity<BowlingScore>()
                .HasOne(bs => bs.Player)
                .WithMany(p => p.BowlingScores)
                .HasForeignKey(bs => bs.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // BowlingScore -> Team
            modelBuilder.Entity<BowlingScore>()
                .HasOne(bs => bs.Team)
                .WithMany(t => t.BowlingScores)
                .HasForeignKey(bs => bs.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // BallByBall -> Match
            modelBuilder.Entity<BallByBall>()
                .HasOne(b => b.Match)
                .WithMany(m => m.BallByBalls)
                .HasForeignKey(b => b.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // BallByBall -> Batsman
            modelBuilder.Entity<BallByBall>()
                .HasOne(b => b.Batsman)
                .WithMany()
                .HasForeignKey(b => b.BatsmanId)
                .OnDelete(DeleteBehavior.Restrict);

            // BallByBall -> Bowler
            modelBuilder.Entity<BallByBall>()
                .HasOne(b => b.Bowler)
                .WithMany()
                .HasForeignKey(b => b.BowlerId)
                .OnDelete(DeleteBehavior.Restrict);

            // BallByBall -> OutBatsman
            modelBuilder.Entity<BallByBall>()
    .HasOne(b => b.OutBatsman)
    .WithMany()
    .HasForeignKey(b => b.OutBatsmanId)
    .OnDelete(DeleteBehavior.Restrict);

            // BallByBall -> Fielder
            modelBuilder.Entity<BallByBall>()
    .HasOne(b => b.Fielder)
    .WithMany()
    .HasForeignKey(b => b.FielderId)
    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
