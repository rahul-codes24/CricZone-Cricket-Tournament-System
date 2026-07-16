








using KplTournament.Web.Data;
using KplTournament.Web.Models;
using KplTournament.Web.Models.ViewModels;
using KplTournament.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KplTournament.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        private int? GetActiveTournamentId(out IActionResult? redirectResult)
        {
            redirectResult = null;
            if (Request.Cookies.TryGetValue("ActiveTournamentId", out var val) && int.TryParse(val, out var id))
            {
                var exists = _db.Tournaments.Any(t => t.Id == id);
                if (exists)
                {
                    return id;
                }
            }

            redirectResult = RedirectToAction("Index", "Landing");
            return null;
        }

        // HOME PAGE
        public async Task<IActionResult> Index()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            ViewBag.TotalTeams = await _db.Teams.CountAsync(t => t.TournamentId == activeId);
            ViewBag.TotalPlayers = await _db.Players.CountAsync(p => p.Team!.TournamentId == activeId);
            ViewBag.TotalMatches = await _db.Matches.CountAsync(m => m.TournamentId == activeId);

            /* =========================
               TOURNAMENT STATS
            ========================= */

            // Total Sixes
            ViewBag.TotalSixes = await _db.BallByBalls
                .CountAsync(b =>
                    b.Match.TournamentId == activeId &&
                    b.IsLegalBall &&
                    !b.IsBye &&
                    !b.IsLegBye &&
                    b.Runs == 6);

            // Total Fours
            ViewBag.TotalFours = await _db.BallByBalls
                .CountAsync(b =>
                    b.Match.TournamentId == activeId &&
                    b.IsLegalBall &&
                    !b.IsBye &&
                    !b.IsLegBye &&
                    b.Runs == 4);

            // Total Wickets
            ViewBag.TotalWickets = await _db.BallByBalls
                .CountAsync(b => b.Match.TournamentId == activeId && b.IsWicket);

            // Show only started / live / completed matches on home page
            ViewBag.RecentMatches = await _db.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Where(m => m.TournamentId == activeId && (m.Status == "Live" || m.Status == "Completed" || m.Status == "Innings Break"))
                .OrderByDescending(m => m.MatchDate)
                .ThenByDescending(m => m.Id)
                .Take(10)
                .ToListAsync();

            return View();
        }

        // VIEW ALL MATCHES
        public async Task<IActionResult> Matches()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            var matches = await _db.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Where(m => m.TournamentId == activeId && (m.Status == "Live" || m.Status == "Completed" || m.Status == "Innings Break"))
                .OrderByDescending(m => m.MatchDate)
                .ThenByDescending(m => m.Id)
                .ToListAsync();

            return View(matches);
        }

        // POINTS TABLE
        public async Task<IActionResult> PointsTable()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            var teams = await _db.Teams.Where(t => t.TournamentId == activeId).ToListAsync();
            var matches = await _db.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Where(m => m.TournamentId == activeId && m.Status == "Completed")
                .ToListAsync();

            // Legal balls per (match, innings) — used to compute true fractional overs
            // (e.g. 4 balls into an over = 4/6 of an over, not ".4").
            var legalBallCounts = await _db.BallByBalls
                .Where(b => b.Match.TournamentId == activeId && b.IsLegalBall)
                .GroupBy(b => new { b.MatchId, b.InningsNumber })
                .Select(g => new { g.Key.MatchId, g.Key.InningsNumber, Count = g.Count() })
                .ToListAsync();

            var allMatchesForRecord = await _db.Matches.Where(m => m.TournamentId == activeId).ToListAsync();

            var pointsTable = teams.Select(t =>
            {
                var teamCompletedMatches = matches.Where(m => m.TeamAId == t.Id || m.TeamBId == t.Id).ToList();

                decimal totalRunsFor = 0, totalOversFor = 0;
                decimal totalRunsAgainst = 0, totalOversAgainst = 0;

                foreach (var m in teamCompletedMatches)
                {
                    bool teamABattedFirst = DidTeamABatFirstForNrr(m);
                    bool isTeamA = m.TeamAId == t.Id;

                    // Which innings number did *this* team bat in?
                    int teamInnings = (isTeamA == teamABattedFirst) ? 1 : 2;
                    int opponentInnings = teamInnings == 1 ? 2 : 1;

                    int teamRuns = isTeamA ? m.TeamAScore : m.TeamBScore;
                    int teamWickets = isTeamA ? m.TeamAWickets : m.TeamBWickets;
                    int opponentRuns = isTeamA ? m.TeamBScore : m.TeamAScore;
                    int opponentWickets = isTeamA ? m.TeamBWickets : m.TeamAWickets;

                    int teamLegalBalls = legalBallCounts
                        .Where(x => x.MatchId == m.Id && x.InningsNumber == teamInnings)
                        .Select(x => x.Count).FirstOrDefault();
                    int opponentLegalBalls = legalBallCounts
                        .Where(x => x.MatchId == m.Id && x.InningsNumber == opponentInnings)
                        .Select(x => x.Count).FirstOrDefault();

                    // Standard NRR rule: if a team is bowled out before facing all its overs,
                    // its overs-faced is still counted as the full allotted overs.
                    decimal oversFaced = teamWickets >= 10
                        ? m.OversPerInnings
                        : teamLegalBalls / 6m;

                    // Similarly for bowling: if the opponent was bowled out, the overs bowled is full allotment.
                    decimal oversBowled = opponentWickets >= 10
                        ? m.OversPerInnings
                        : opponentLegalBalls / 6m;

                    totalRunsFor += teamRuns;
                    totalOversFor += oversFaced;
                    totalRunsAgainst += opponentRuns;
                    totalOversAgainst += oversBowled;
                }

                decimal nrr = 0;
                if (totalOversFor > 0 && totalOversAgainst > 0)
                {
                    nrr = Math.Round((totalRunsFor / totalOversFor) - (totalRunsAgainst / totalOversAgainst), 3);
                }

                return new PointsTableRowVm
                {
                    TeamName = t.Name,
                    Played = allMatchesForRecord.Count(m => m.TeamAId == t.Id || m.TeamBId == t.Id),
                    Won = allMatchesForRecord.Count(m => m.WinnerTeamId == t.Id),
                    Lost = allMatchesForRecord.Count(m =>
                        (m.TeamAId == t.Id || m.TeamBId == t.Id) &&
                        m.WinnerTeamId != null &&
                        m.WinnerTeamId != t.Id),
                    Points = allMatchesForRecord.Count(m => m.WinnerTeamId == t.Id) * 2,
                    NetRunRate = nrr
                };
            })
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.NetRunRate)
            .ThenBy(x => x.TeamName)
            .ToList();

            return View(pointsTable);
        }

        private bool DidTeamABatFirstForNrr(Match match)
        {
            string tossWinner = (match.TossWinner ?? string.Empty).Trim();
            string tossDecision = (match.TossDecision ?? string.Empty).Trim();

            bool teamAWonToss = string.Equals(tossWinner, match.TeamA?.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            bool teamBWonToss = string.Equals(tossWinner, match.TeamB?.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            bool choseBat = string.Equals(tossDecision, "Bat", StringComparison.OrdinalIgnoreCase);
            bool choseBowl = string.Equals(tossDecision, "Bowl", StringComparison.OrdinalIgnoreCase);

            if (teamAWonToss && choseBat) return true;
            if (teamAWonToss && choseBowl) return false;
            if (teamBWonToss && choseBat) return false;
            if (teamBWonToss && choseBowl) return true;

            return true;
        }

        // TEAMS PAGE
        public async Task<IActionResult> Teams()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            var teams = await _db.Teams
                .Include(t => t.Players)
                .Where(t => t.TournamentId == activeId)
                .ToListAsync();

            return View(teams);
        }

        // OLD SIMPLE MATCH SCORECARD
        public async Task<IActionResult> MatchScorecard(int id)
        {
            var match = await _db.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.ManOfTheMatchPlayer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
                return NotFound();

            return View(match);
        }

        // NEW FULL PUBLIC SCORECARD
        public async Task<IActionResult> ViewScorecard(int id)
        {
            var vm = await BuildScorecardViewModel(id);
            if (vm == null)
            {
                TempData["Error"] = "Match not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        // Downloadable PDF version of the same scorecard, generated on demand
        // (not stored - always reflects the latest data for that match).
        public async Task<IActionResult> DownloadScorecardPdf(int id)
        {
            var vm = await BuildScorecardViewModel(id);
            if (vm == null)
            {
                TempData["Error"] = "Match not found.";
                return RedirectToAction(nameof(Index));
            }

            var pdfBytes = KplTournament.Web.Services.ScorecardPdfService.Generate(vm);
            var teamAName = vm.Match.TeamA?.Name ?? "TeamA";
            var teamBName = vm.Match.TeamB?.Name ?? "TeamB";
            var fileName = $"{teamAName}_vs_{teamBName}_Scorecard.pdf".Replace(' ', '_');

            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<ScorecardViewModel?> BuildScorecardViewModel(int id)
        {
            var match = await _db.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
            {
                return null;
            }

            var allBalls = await _db.BallByBalls
                .Include(b => b.Batsman)
                .Include(b => b.Bowler)
                .Include(b => b.OutBatsman)
                .Include(b => b.Fielder)
                .Where(b => b.MatchId == id)
                .OrderBy(b => b.InningsNumber)
                .ThenBy(b => b.OverNumber)
                .ThenBy(b => b.BallNumber)
                .ToListAsync();

            var teamAPlayers = await _db.Players
                .Where(p => p.TeamId == match.TeamAId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var teamBPlayers = await _db.Players
                .Where(p => p.TeamId == match.TeamBId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            int innings1BattingTeamId = GetFirstInningsBattingTeamId(match);
            int innings2BattingTeamId = GetSecondInningsBattingTeamId(match);

            var innings1Balls = allBalls.Where(b => b.InningsNumber == 1).ToList();
            var innings2Balls = allBalls.Where(b => b.InningsNumber == 2).ToList();

            return new ScorecardViewModel
            {
                Match = match,
                Innings1 = BuildInningsScorecard(
                    inningsNumber: 1,
                    battingTeamId: innings1BattingTeamId,
                    battingTeamName: innings1BattingTeamId == match.TeamAId
                        ? (match.TeamA?.Name ?? "Team A")
                        : (match.TeamB?.Name ?? "Team B"),
                    balls: innings1Balls,
                    battingPlayers: innings1BattingTeamId == match.TeamAId ? teamAPlayers : teamBPlayers,
                    bowlingPlayers: innings1BattingTeamId == match.TeamAId ? teamBPlayers : teamAPlayers
                ),
                Innings2 = BuildInningsScorecard(
                    inningsNumber: 2,
                    battingTeamId: innings2BattingTeamId,
                    battingTeamName: innings2BattingTeamId == match.TeamAId
                        ? (match.TeamA?.Name ?? "Team A")
                        : (match.TeamB?.Name ?? "Team B"),
                    balls: innings2Balls,
                    battingPlayers: innings2BattingTeamId == match.TeamAId ? teamAPlayers : teamBPlayers,
                    bowlingPlayers: innings2BattingTeamId == match.TeamAId ? teamBPlayers : teamAPlayers
                ),
                Balls = allBalls.Select(b => new BallByBallViewModel
                {
                    InningsNumber = b.InningsNumber,
                    OverText = $"{b.OverNumber}.{b.BallNumber}",
                    BatsmanName = b.Batsman?.Name ?? "-",
                    BowlerName = b.Bowler?.Name ?? "-",
                    ResultText = !string.IsNullOrWhiteSpace(b.BallText) ? b.BallText : GetBallTextFromExistingBall(b)
                }).ToList()
            };
        }

        // AWARDS MAIN PAGE
        public async Task<IActionResult> Awards()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            ViewBag.BestBatsman = await _db.BattingScores
                .Where(x => x.Match.TournamentId == activeId)
                .Include(x => x.Player)
                .GroupBy(x => new { x.PlayerId, x.Player!.Name })
                .Select(g => new
                {
                    PlayerName = g.Key.Name,
                    TotalRuns = g.Sum(x => x.Runs)
                })
                .OrderByDescending(x => x.TotalRuns)
                .FirstOrDefaultAsync();

            ViewBag.BestBowler = await _db.BowlingScores
                .Include(x => x.Player)
                .Where(x => x.Match.TournamentId == activeId)
                .GroupBy(x => new { x.PlayerId, x.Player!.Name })
                .Select(g => new
                {
                    PlayerName = g.Key.Name,
                    TotalWickets = g.Sum(x => x.Wickets)
                })
                .OrderByDescending(x => x.TotalWickets)
                .FirstOrDefaultAsync();

            ViewBag.MatchAwards = await _db.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.ManOfTheMatchPlayer)
                .Where(m => m.TournamentId == activeId && m.ManOfTheMatchPlayerId != null)
                .Select(m => new
                {
                    MatchTitle = m.TeamA!.Name + " vs " + m.TeamB!.Name,
                    MatchDate = m.MatchDate,
                    ManOfTheMatch = m.ManOfTheMatchPlayer!.Name
                })
                .ToListAsync();

            ViewBag.EmergingPlayer = "Coming Soon";
            ViewBag.BestFielder = "Coming Soon";

            return View();
        }

        // TOURNAMENT-WIDE LEADERBOARD — combines batting, bowling, fielding and MOM awards
        // into one weighted score across every completed match, not just a single game.
        public async Task<IActionResult> PlayerOfTheSeries()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            var battingByPlayer = await _db.BallByBalls
                .Where(b => b.Match.TournamentId == activeId && b.BatsmanId != null)
                .GroupBy(b => new { b.BatsmanId, PlayerName = b.Batsman!.Name })
                .Select(g => new
                {
                    PlayerId = g.Key.BatsmanId!.Value,
                    PlayerName = g.Key.PlayerName,
                    Runs = g.Where(x => !x.IsBye && !x.IsLegBye).Sum(x => x.Runs),
                    Matches = g.Select(x => x.MatchId).Distinct().Count()
                })
                .ToListAsync();

            var bowlingByPlayer = await _db.BallByBalls
                .Where(b => b.Match.TournamentId == activeId && b.BowlerId != null)
                .GroupBy(b => new { b.BowlerId, PlayerName = b.Bowler!.Name })
                .Select(g => new
                {
                    PlayerId = g.Key.BowlerId!.Value,
                    Wickets = g.Count(x => x.IsWicket),
                    Matches = g.Select(x => x.MatchId).Distinct().Count()
                })
                .ToListAsync();

            var fieldingByPlayer = await _db.BallByBalls
                .Where(b => b.Match.TournamentId == activeId && b.FielderId != null && b.IsWicket)
                .GroupBy(b => b.FielderId)
                .Select(g => new
                {
                    PlayerId = g.Key!.Value,
                    Catches = g.Count()
                })
                .ToListAsync();

            var momCounts = await _db.Matches
                .Where(m => m.ManOfTheMatchPlayerId != null)
                .GroupBy(m => m.ManOfTheMatchPlayerId)
                .Select(g => new { PlayerId = g.Key!.Value, Count = g.Count() })
                .ToListAsync();

            var playerIds = battingByPlayer.Select(x => x.PlayerId)
                .Union(bowlingByPlayer.Select(x => x.PlayerId))
                .Distinct()
                .ToList();

            var rows = playerIds.Select(id =>
            {
                var bat = battingByPlayer.FirstOrDefault(x => x.PlayerId == id);
                var bowl = bowlingByPlayer.FirstOrDefault(x => x.PlayerId == id);
                var field = fieldingByPlayer.FirstOrDefault(x => x.PlayerId == id);
                var mom = momCounts.FirstOrDefault(x => x.PlayerId == id);

                int runs = bat?.Runs ?? 0;
                int wickets = bowl?.Wickets ?? 0;
                int catches = field?.Catches ?? 0;
                int momCount = mom?.Count ?? 0;
                int matches = new[] { bat?.Matches ?? 0, bowl?.Matches ?? 0 }.Max();

                // Weighted "series impact" score — same weighting philosophy as the
                // per-match MOM formula (PlayerMatchStats.PerformanceScore), plus a
                // bonus for MOM awards since Player of the Series should reward
                // consistent match-winning performances, not just raw totals.
                double score = (runs * 1.0) + (wickets * 25.0) + (catches * 10.0) + (momCount * 50.0);

                return new
                {
                    PlayerId = id,
                    PlayerName = bat?.PlayerName ?? "Unknown",
                    Runs = runs,
                    Wickets = wickets,
                    Catches = catches,
                    MomCount = momCount,
                    Matches = matches,
                    Score = score
                };
            })
            .OrderByDescending(x => x.Score)
            .Take(10)
            .ToList();

            // Resolve player names properly (batting-only won't have a name from the query above
            // when a player only bowled/fielded).
            var namesById = await _db.Players
                .Where(p => playerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var finalRows = rows.Select(r => new
            {
                r.PlayerId,
                PlayerName = namesById.TryGetValue(r.PlayerId, out var n) ? n : r.PlayerName,
                r.Runs,
                r.Wickets,
                r.Catches,
                r.MomCount,
                r.Matches,
                r.Score
            }).ToList();

            return View(finalRows);
        }

        public async Task<IActionResult> OrangeCap()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            var data = await _db.BallByBalls
                .Include(b => b.Batsman)
                .Where(b => b.Match.TournamentId == activeId && b.BatsmanId != null)
                .GroupBy(b => new
                {
                    b.BatsmanId,
                    PlayerName = b.Batsman!.Name
                })
                .Select(g => new
                {
                    PlayerName = g.Key.PlayerName,
                    Runs = g.Where(x => !x.IsBye && !x.IsLegBye).Sum(x => x.Runs),
                    Balls = g.Count(x => x.IsLegalBall),
                    Fours = g.Count(x => x.IsLegalBall && !x.IsBye && !x.IsLegBye && x.Runs == 4),
                    Sixes = g.Count(x => x.IsLegalBall && !x.IsBye && !x.IsLegBye && x.Runs == 6)
                })
                .OrderByDescending(x => x.Runs)
                .ThenBy(x => x.Balls)
                .Take(10)
                .ToListAsync();

            return View(data);
        }

        public async Task<IActionResult> PurpleCap()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            var data = await _db.BallByBalls
                .Include(b => b.Bowler)
                .Where(b => b.Match.TournamentId == activeId && b.BowlerId != null)
                .GroupBy(b => new
                {
                    b.BowlerId,
                    PlayerName = b.Bowler!.Name
                })
                .Select(g => new
                {
                    PlayerName = g.Key.PlayerName,
                    Wickets = g.Count(x => x.IsWicket),
                    Balls = g.Count(x => x.IsLegalBall),
                    RunsGiven = g.Sum(x =>
                        x.IsBye || x.IsLegBye
                            ? 0
                            : x.Runs + x.Extras),
                    Wides = g.Where(x => x.IsWide).Sum(x => x.Extras),
                    NoBalls = g.Where(x => x.IsNoBall).Sum(x => x.Extras)
                })
                .OrderByDescending(x => x.Wickets)
                .ThenBy(x => x.RunsGiven)
                .Take(10)
                .ToListAsync();

            return View(data);
        }


        public async Task<IActionResult> MostSixes()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            var data = await _db.BallByBalls
                .Include(b => b.Batsman)
                .Where(b => b.Match.TournamentId == activeId && b.BatsmanId != null)
                .GroupBy(b => new
                {
                    b.BatsmanId,
                    PlayerName = b.Batsman!.Name
                })
                .Select(g => new
                {
                    PlayerName = g.Key.PlayerName,
                    Sixes = g.Count(x => x.IsLegalBall && !x.IsBye && !x.IsLegBye && x.Runs == 6),
                    Runs = g.Where(x => !x.IsBye && !x.IsLegBye).Sum(x => x.Runs),
                    Balls = g.Count(x => x.IsLegalBall)
                })
                .Where(x => x.Sixes > 0)
                .OrderByDescending(x => x.Sixes)
                .ThenByDescending(x => x.Runs)
                .Take(10)
                .ToListAsync();

            return View(data);
        }

        public async Task<IActionResult> ManOfTheMatch()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            var data = await _db.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.ManOfTheMatchPlayer)
                .Where(m => m.TournamentId == activeId && m.ManOfTheMatchPlayerId != null)
                .Select(m => new
                {
                    MatchTitle = m.TeamA!.Name + " vs " + m.TeamB!.Name,
                    Player = m.ManOfTheMatchPlayer!.Name,
                    Date = m.MatchDate
                })
                .ToListAsync();

            return View(data);
        }

        public IActionResult EmergingPlayer()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;
            ViewBag.Player = "Coming Soon";
            return View();
        }

        public async Task<IActionResult> BestFielder()
        {
            var activeId = GetActiveTournamentId(out var redirect);
            if (redirect != null) return redirect;

            var balls = await _db.BallByBalls
                .Include(b => b.Fielder)
                .Where(b => b.Match.TournamentId == activeId && b.FielderId != null && b.IsWicket)
                .ToListAsync();

            var data = balls
                .GroupBy(b => new
                {
                    b.FielderId,
                    PlayerName = b.Fielder!.Name
                })
                .Select(g =>
                {
                    int catches = g.Count(x =>
                        !string.IsNullOrWhiteSpace(x.BallText) &&
                        x.BallText.ToLower().Contains("catch"));

                    int runOuts = g.Count(x =>
                        !string.IsNullOrWhiteSpace(x.BallText) &&
                        x.BallText.ToLower().Contains("run out"));

                    int stumpings = g.Count(x =>
                        !string.IsNullOrWhiteSpace(x.BallText) &&
                        x.BallText.ToLower().Contains("stump"));

                    int directHits = g.Count(x =>
                        !string.IsNullOrWhiteSpace(x.BallText) &&
                        x.BallText.ToLower().Contains("direct"));

                    int fieldingPoints =
                        (catches * 3) +
                        (runOuts * 4) +
                        (stumpings * 4) +
                        (directHits * 5);

                    return new
                    {
                        PlayerName = g.Key.PlayerName,
                        Catches = catches,
                        RunOuts = runOuts,
                        Stumpings = stumpings,
                        DirectHits = directHits,
                        FieldingPoints = fieldingPoints
                    };
                })
                .Where(x => x.FieldingPoints > 0)
                .OrderByDescending(x => x.FieldingPoints)
                .ThenByDescending(x => x.DirectHits)
                .ThenByDescending(x => x.RunOuts)
                .ThenByDescending(x => x.Catches)
                .Take(5)
                .ToList();

            return View(data);
        }

        // PLAYER CAREER STATS — aggregated across every match the player has played
        public async Task<IActionResult> PlayerCareerStats(int id)
        {
            var player = await _db.Players
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
                return NotFound();

            var battingBalls = await _db.BallByBalls
                .Where(b => b.BatsmanId == id)
                .ToListAsync();

            var bowlingBalls = await _db.BallByBalls
                .Where(b => b.BowlerId == id)
                .ToListAsync();

            var fieldingBalls = await _db.BallByBalls
                .Where(b => b.FielderId == id && b.IsWicket)
                .ToListAsync();

            var vm = new PlayerCareerStatsVm
            {
                PlayerId = player.Id,
                PlayerName = player.Name,
                TeamName = player.Team?.Name ?? "",
                PhotoPath = player.PhotoPath
            };

            vm.Matches = battingBalls.Select(b => b.MatchId)
                .Union(bowlingBalls.Select(b => b.MatchId))
                .Distinct()
                .Count();

            // Batting — grouped per match+innings to get per-innings scores (for highest score, 50s, 100s)
            var battingByInnings = battingBalls
                .GroupBy(b => new { b.MatchId, b.InningsNumber })
                .Select(g => new
                {
                    Runs = g.Where(b => !b.IsBye && !b.IsLegBye).Sum(b => b.Runs),
                    Balls = g.Count(b => b.IsLegalBall),
                    Fours = g.Count(b => b.IsLegalBall && !b.IsBye && !b.IsLegBye && b.Runs == 4),
                    Sixes = g.Count(b => b.IsLegalBall && !b.IsBye && !b.IsLegBye && b.Runs == 6),
                    WasOut = g.Any(b => b.OutBatsmanId == id || (b.IsWicket && b.BatsmanId == id))
                })
                .ToList();

            vm.Innings = battingByInnings.Count;
            vm.Runs = battingByInnings.Sum(x => x.Runs);
            vm.BallsFaced = battingByInnings.Sum(x => x.Balls);
            vm.Fours = battingByInnings.Sum(x => x.Fours);
            vm.Sixes = battingByInnings.Sum(x => x.Sixes);
            vm.Dismissals = battingByInnings.Count(x => x.WasOut);
            vm.HighestScore = battingByInnings.Any() ? battingByInnings.Max(x => x.Runs) : 0;
            vm.Fifties = battingByInnings.Count(x => x.Runs >= 50 && x.Runs < 100);
            vm.Hundreds = battingByInnings.Count(x => x.Runs >= 100);

            // Bowling — grouped per match+innings to find best bowling figures
            var bowlingByInnings = bowlingBalls
                .GroupBy(b => new { b.MatchId, b.InningsNumber })
                .Select(g => new
                {
                    Wickets = g.Count(b => b.IsWicket),
                    RunsGiven = g.Sum(b => (b.IsBye || b.IsLegBye) ? 0 : b.Runs + b.Extras)
                })
                .ToList();

            vm.WicketsTaken = bowlingByInnings.Sum(x => x.Wickets);
            vm.RunsConceded = bowlingByInnings.Sum(x => x.RunsGiven);
            vm.BallsBowled = bowlingBalls.Count(b => b.IsLegalBall);

            var best = bowlingByInnings
                .OrderByDescending(x => x.Wickets)
                .ThenBy(x => x.RunsGiven)
                .FirstOrDefault();
            if (best != null && best.Wickets > 0)
                vm.BestBowling = $"{best.Wickets}/{best.RunsGiven}";

            // Fielding
            vm.Catches = fieldingBalls.Count(b =>
                !string.IsNullOrWhiteSpace(b.BallText) && b.BallText.ToLower().Contains("catch"));
            vm.RunOuts = fieldingBalls.Count(b =>
                !string.IsNullOrWhiteSpace(b.BallText) && b.BallText.ToLower().Contains("run out"));

            return View(vm);
        }

        // HEAD TO HEAD — pick two teams and compare their history
        public async Task<IActionResult> HeadToHead(int? teamAId, int? teamBId)
        {
            ViewBag.Teams = await _db.Teams.OrderBy(t => t.Name).ToListAsync();

            if (!teamAId.HasValue || !teamBId.HasValue || teamAId == teamBId)
                return View();

            var teamA = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamAId);
            var teamB = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamBId);

            if (teamA == null || teamB == null)
                return View();

            var matches = await _db.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.WinnerTeam)
                .Where(m => m.Status == "Completed" &&
                    ((m.TeamAId == teamAId && m.TeamBId == teamBId) ||
                     (m.TeamAId == teamBId && m.TeamBId == teamAId)))
                .OrderByDescending(m => m.MatchDate)
                .ToListAsync();

            var vm = new HeadToHeadVm
            {
                TeamA = teamA,
                TeamB = teamB,
                Matches = matches,
                MatchesPlayed = matches.Count,
                TeamAWins = matches.Count(m => m.WinnerTeamId == teamAId),
                TeamBWins = matches.Count(m => m.WinnerTeamId == teamBId),
                NoResult = matches.Count(m => m.WinnerTeamId == null),
                TeamAHighestScore = matches
                    .Select(m => m.TeamAId == teamAId ? m.TeamAScore : m.TeamBScore)
                    .DefaultIfEmpty(0)
                    .Max(),
                TeamBHighestScore = matches
                    .Select(m => m.TeamAId == teamBId ? m.TeamAScore : m.TeamBScore)
                    .DefaultIfEmpty(0)
                    .Max()
            };

            return View(vm);
        }

        private InningsScorecardViewModel BuildInningsScorecard(
            int inningsNumber,
            int battingTeamId,
            string battingTeamName,
            List<BallByBall> balls,
            List<Player> battingPlayers,
            List<Player> bowlingPlayers)
        {
            var vm = new InningsScorecardViewModel
            {
                InningsNumber = inningsNumber,
                BattingTeamId = battingTeamId,
                BattingTeamName = battingTeamName
            };

            if (balls == null || !balls.Any())
            {
                vm.TotalRuns = 0;
                vm.Wickets = 0;
                vm.Overs = 0;
                vm.RunRate = 0;
                vm.BattingSummary = "0-0 (0.0 Ov)";
                return vm;
            }

            vm.TotalRuns = balls.Sum(GetTeamRunsFromBall);
            vm.Wickets = balls.Count(b => b.IsWicket);

            vm.Byes = balls.Where(b => b.IsBye).Sum(b => b.Extras);
            vm.LegByes = balls.Where(b => b.IsLegBye).Sum(b => b.Extras);
            vm.Wides = balls.Where(b => b.IsWide).Sum(b => b.Extras);
            vm.NoBalls = balls.Where(b => b.IsNoBall).Sum(b => b.Extras);

            int legalBalls = balls.Count(b => b.IsLegalBall);
            vm.Overs = Convert.ToDecimal($"{legalBalls / 6}.{legalBalls % 6}");
            vm.RunRate = legalBalls > 0 ? Math.Round((vm.TotalRuns * 6.0) / legalBalls, 2) : 0;
            vm.BattingSummary = $"{vm.TotalRuns}-{vm.Wickets} ({legalBalls / 6}.{legalBalls % 6} Ov)";

            vm.Batting = BuildBattingRows(balls, battingPlayers);
            vm.Bowling = BuildBowlingRows(balls, bowlingPlayers);
            vm.FallOfWickets = BuildFallOfWickets(balls);

            return vm;
        }

        private List<BattingRowViewModel> BuildBattingRows(List<BallByBall> balls, List<Player> battingPlayers)
        {
            var rows = new List<BattingRowViewModel>();

            foreach (var player in battingPlayers)
            {
                var playerBalls = balls.Where(b => b.BatsmanId == player.Id).ToList();

                if (!playerBalls.Any())
                    continue;

                int runs = playerBalls
                    .Where(b => !b.IsBye && !b.IsLegBye)
                    .Sum(b => b.Runs);

                int ballsFaced = playerBalls.Count(b => b.IsLegalBall);
                int fours = playerBalls.Count(b => b.IsLegalBall && !b.IsBye && !b.IsLegBye && b.Runs == 4);
                int sixes = playerBalls.Count(b => b.IsLegalBall && !b.IsBye && !b.IsLegBye && b.Runs == 6);

                var wicketBall = playerBalls.LastOrDefault(b =>
                    b.OutBatsmanId == player.Id || (b.IsWicket && b.BatsmanId == player.Id));

                rows.Add(new BattingRowViewModel
                {
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    Status = wicketBall != null
                        ? (!string.IsNullOrWhiteSpace(wicketBall.BallText) ? wicketBall.BallText : "out")
                        : "not out",
                    Runs = runs,
                    Balls = ballsFaced,
                    Fours = fours,
                    Sixes = sixes,
                    StrikeRate = ballsFaced > 0 ? Math.Round((runs * 100.0) / ballsFaced, 2) : 0
                });
            }

            return rows
                .OrderByDescending(x => x.Runs)
                .ThenBy(x => x.Balls)
                .ToList();
        }

        private List<BowlingRowViewModel> BuildBowlingRows(List<BallByBall> balls, List<Player> bowlingPlayers)
        {
            var rows = new List<BowlingRowViewModel>();

            foreach (var player in bowlingPlayers)
            {
                var bowlerBalls = balls.Where(b => b.BowlerId == player.Id).ToList();

                if (!bowlerBalls.Any())
                    continue;

                int legalBalls = bowlerBalls.Count(b => b.IsLegalBall);

                int runsGiven = bowlerBalls.Sum(b =>
                {
                    if (b.IsBye || b.IsLegBye)
                        return 0;

                    return b.Runs + b.Extras;
                });

                int wickets = bowlerBalls.Count(b => b.IsWicket);
                int noBalls = bowlerBalls.Where(b => b.IsNoBall).Sum(b => b.Extras);
                int wides = bowlerBalls.Where(b => b.IsWide).Sum(b => b.Extras);

                int maidens = bowlerBalls
                    .Where(b => b.IsLegalBall)
                    .GroupBy(b => b.OverNumber)
                    .Count(g =>
                    {
                        int overRuns = g.Sum(x =>
                        {
                            if (x.IsBye || x.IsLegBye)
                                return 0;

                            return x.Runs + x.Extras;
                        });

                        return overRuns == 0;
                    });

                rows.Add(new BowlingRowViewModel
                {
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    BallsBowled = legalBalls,
                    OversText = $"{legalBalls / 6}.{legalBalls % 6}",
                    Maidens = maidens,
                    RunsGiven = runsGiven,
                    Wickets = wickets,
                    NoBalls = noBalls,
                    Wides = wides,
                    Economy = legalBalls > 0 ? Math.Round((runsGiven * 6.0) / legalBalls, 2) : 0
                });
            }

            return rows
                .OrderBy(x => x.PlayerName)
                .ToList();
        }

        private List<FallOfWicketViewModel> BuildFallOfWickets(List<BallByBall> balls)
        {
            var result = new List<FallOfWicketViewModel>();
            int runningScore = 0;

            foreach (var ball in balls.OrderBy(b => b.OverNumber).ThenBy(b => b.BallNumber))
            {
                runningScore += GetTeamRunsFromBall(ball);

                if (ball.IsWicket)
                {
                    result.Add(new FallOfWicketViewModel
                    {
                        PlayerName = ball.OutBatsman?.Name ?? ball.Batsman?.Name ?? "-",
                        WicketText = !string.IsNullOrWhiteSpace(ball.BallText) ? ball.BallText : "out",
                        OverText = $"{ball.OverNumber}.{ball.BallNumber}",
                        TeamScoreAtFall = runningScore
                    });
                }
            }

            return result;
        }

        private int GetTeamRunsFromBall(BallByBall ball)
        {
            if (ball.IsBye || ball.IsLegBye)
                return ball.Extras;

            return ball.Runs + ball.Extras;
        }

        private string GetBallTextFromExistingBall(BallByBall ball)
        {
            if (!string.IsNullOrWhiteSpace(ball.BallText))
                return ball.BallText;

            if (ball.IsWicket)
                return "W";

            if (ball.IsWide)
                return ball.Extras == 1 ? "Wd" : $"Wd{ball.Extras}";

            if (ball.IsNoBall)
                return ball.Runs == 0 ? "Nb" : $"Nb+{ball.Runs}";

            if (ball.IsLegBye)
                return $"LB{ball.Extras}";

            if (ball.IsBye)
                return $"B{ball.Extras}";

            return ball.Runs.ToString();
        }

        private bool DidTeamABatFirst(Match match)
        {
            string tossWinner = (match.TossWinner ?? string.Empty).Trim();
            string tossDecision = (match.TossDecision ?? string.Empty).Trim();

            bool teamAWonToss = string.Equals(
                tossWinner,
                match.TeamA?.Name ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);

            bool teamBWonToss = string.Equals(
                tossWinner,
                match.TeamB?.Name ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);

            bool choseBat = string.Equals(tossDecision, "Bat", StringComparison.OrdinalIgnoreCase);
            bool choseBowl = string.Equals(tossDecision, "Bowl", StringComparison.OrdinalIgnoreCase);

            if (teamAWonToss && choseBat)
                return true;

            if (teamAWonToss && choseBowl)
                return false;

            if (teamBWonToss && choseBat)
                return false;

            if (teamBWonToss && choseBowl)
                return true;

            return true;
        }

        private int GetFirstInningsBattingTeamId(Match match)
        {
            return DidTeamABatFirst(match) ? match.TeamAId : match.TeamBId;
        }

        private int GetSecondInningsBattingTeamId(Match match)
        {
            return DidTeamABatFirst(match) ? match.TeamBId : match.TeamAId;
        }
    }
}