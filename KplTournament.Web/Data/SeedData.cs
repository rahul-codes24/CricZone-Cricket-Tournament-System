using System.Security.Cryptography;
using System.Text;
using KplTournament.Web.Models;

namespace KplTournament.Web.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (!db.AdminUsers.Any())
        {
            var salt = GenerateSalt();
            db.AdminUsers.Add(new AdminUser
            {
                Email = "admin@kpl.com",
                PasswordSalt = salt,
                PasswordHash = Hash("admin123", salt)
            });
        }

        if (!db.Tournaments.Any())
        {
            db.Tournaments.Add(new Tournament
            {
                Name = "CricZone Tester Tournament",
                Description = "Seeded tournament for verification and testing.",
                AdminEmail = "admin@kpl.com",
                Venue = "CricZone Arena",
                StartDate = DateTime.Today.AddDays(-5),
                EndDate = DateTime.Today.AddDays(5),
                Status = "Active"
            });
            db.SaveChanges();
        }

        var activeTournament = db.Tournaments.FirstOrDefault(t => t.Name == "CricZone Tester Tournament");
        var activeTournamentId = activeTournament?.Id;

        if (!db.Teams.Any())
        {
            var teams = new List<Team>
            {
                new() { Name = "DAKKHAN TIGERS", ShortName = "DT", OwnerName = "SUNIL KARAMBALAKAR", PrimaryColor = "#2563eb", TournamentId = activeTournamentId },
                new() { Name = "MARATHA ELEVEN", ShortName = "ME", OwnerName = "SAHDEV GAWADE", PrimaryColor = "#16a34a", TournamentId = activeTournamentId },
                new() { Name = "SWARAJYA FIGHTERS", ShortName = "SF", OwnerName = "ANIL KARAMBALKAR", PrimaryColor = "#dc2626", TournamentId = activeTournamentId },
                new() { Name = "SHIVNERI WARRIORS", ShortName = "SW", OwnerName = "LAXMAN KHAVNEWADKAR", PrimaryColor = "#7c3aed", TournamentId = activeTournamentId },
                new() { Name = "HINDAVI HITTERS", ShortName = "HH", OwnerName = "SANDIP MOHITE", PrimaryColor = "#ea580c", TournamentId = activeTournamentId }
            };
            db.Teams.AddRange(teams);
            db.SaveChanges();

            foreach (var team in db.Teams.ToList())
            {
                for (int i = 1; i <= 15; i++)
                {
                    db.Players.Add(new Player
                    {
                        Name = $"{team.ShortName} Player {i}",
                        Role = i <= 5 ? "Batsman" : i <= 10 ? "Bowler" : "All-Rounder",
                        TeamId = team.Id,
                        IsCaptain = i == 1,
                        IsViceCaptain = i == 2
                    });
                }
            }
            db.SaveChanges();
        }

        if (!db.Matches.Any())
        {
            var teams = db.Teams.ToList();
            var p1 = db.Players.First();
            var p2 = db.Players.Skip(16).First();
            var match = new Match
            {
                MatchDate = DateTime.Today,
                Venue = "Village Ground",
                OversPerInnings = 10,
                TeamAId = teams[0].Id,
                TeamBId = teams[1].Id,
                TossWinner = teams[0].Name,
                TossDecision = "Bat",
                TeamAScore = 96,
                TeamAWickets = 6,
                TeamAOvers = 10,
                TeamBScore = 89,
                TeamBWickets = 8,
                TeamBOvers = 10,
                Result = $"{teams[0].Name} won by 7 runs",
                WinnerTeamId = teams[0].Id,
                ManOfTheMatchPlayerId = p1.Id,
                Status = "Completed",
                IsMatchCompleted = true,
                TournamentId = activeTournamentId
            };
            db.Matches.Add(match);
            db.SaveChanges();

            db.BattingScores.AddRange(
                new BattingScore { MatchId = match.Id, PlayerId = p1.Id, TeamId = teams[0].Id, Runs = 45, Balls = 28, Fours = 5, Sixes = 2, IsOut = true },
                new BattingScore { MatchId = match.Id, PlayerId = p2.Id, TeamId = teams[1].Id, Runs = 39, Balls = 30, Fours = 4, Sixes = 1, IsOut = true }
            );

            db.BowlingScores.AddRange(
                new BowlingScore { MatchId = match.Id, PlayerId = p1.Id, TeamId = teams[0].Id, Overs = 2, RunsGiven = 14, Wickets = 1, Maidens = 0 },
                new BowlingScore { MatchId = match.Id, PlayerId = p2.Id, TeamId = teams[1].Id, Overs = 2, RunsGiven = 16, Wickets = 2, Maidens = 0 }
            );
        }

        db.SaveChanges();
    }

    /// <summary>
    /// Generates a cryptographically random salt, unique per user.
    /// </summary>
    public static string GenerateSalt()
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToHexString(saltBytes);
    }

    /// <summary>
    /// Salted, iterated PBKDF2 hash (100k rounds, SHA-256).
    /// Replaces the old unsalted single-round SHA-256 hash, which was
    /// vulnerable to rainbow-table and brute-force attacks.
    /// </summary>
    public static string Hash(string input, string salt)
    {
        var saltBytes = Convert.FromHexString(salt);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(input),
            saltBytes,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        return Convert.ToHexString(hashBytes);
    }
}
