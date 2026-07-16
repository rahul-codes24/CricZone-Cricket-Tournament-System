
using KplTournament.Web.Data;
using KplTournament.Web.Hubs;
using KplTournament.Web.Models;
using KplTournament.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KplTournament.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHubContext<MatchHub> _hub;

    public AdminController(AppDbContext db, IHubContext<MatchHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    /// <summary>
    /// Tells every client viewing this match (public scorecard + admin live scoring)
    /// to refresh, right after a scoring action has been saved. Fire-and-forget-safe:
    /// if no one is connected this is just a no-op broadcast.
    /// </summary>
    private Task NotifyMatchUpdated(int matchId) =>
        _hub.Clients.Group(MatchHub.GroupName(matchId)).SendAsync("MatchUpdated");

    private int? GetActiveTournamentId(out IActionResult? redirectResult)
    {
        redirectResult = null;
        if (Request.Cookies.TryGetValue("ActiveTournamentId", out var val) && int.TryParse(val, out var id))
        {
            var email = User.Identity.Name;
            var belongs = _db.Tournaments.Any(t => t.Id == id && t.AdminEmail == email);
            if (belongs)
            {
                return id;
            }
        }

        redirectResult = RedirectToAction("Tournaments");
        return null;
    }

    public async Task<IActionResult> Index()
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        ViewBag.TeamCount = await _db.Teams.CountAsync(t => t.TournamentId == activeId);
        ViewBag.PlayerCount = await _db.Players.CountAsync(p => p.Team!.TournamentId == activeId);
        ViewBag.MatchCount = await _db.Matches.CountAsync(m => m.TournamentId == activeId);

        var matches = await _db.Matches
            .Include(x => x.TeamA)
            .Include(x => x.TeamB)
            .Where(x => x.TournamentId == activeId)
            .OrderByDescending(x => x.MatchDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        return View(matches);
    }

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

    [HttpGet]
    public IActionResult CreateTeam()
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        return View(new Team { TournamentId = activeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeam(Team team, IFormFile? logoFile)
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        team.TournamentId = activeId;
        ModelState.Remove("Tournament");

        if (!ModelState.IsValid)
            return View(team);

        if (logoFile != null && logoFile.Length > 0)
        {
            var logoPath = await SaveTeamLogoAsync(logoFile);
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                ModelState.AddModelError("", "Only JPG, JPEG, PNG, and WEBP files up to 2 MB are allowed.");
                return View(team);
            }

            team.LogoPath = logoPath;
        }

        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Team created successfully.";
        return RedirectToAction(nameof(Teams));
    }

    [HttpGet]
    public async Task<IActionResult> EditTeam(int id)
    {
        var team = await _db.Teams.FindAsync(id);

        if (team == null)
            return NotFound();

        return View(team);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTeam(Team team, IFormFile? logoFile)
    {
        if (!ModelState.IsValid)
            return View(team);

        var existingTeam = await _db.Teams.FindAsync(team.Id);

        if (existingTeam == null)
            return NotFound();

        existingTeam.Name = team.Name;
        existingTeam.ShortName = team.ShortName;
        existingTeam.OwnerName = team.OwnerName;
        existingTeam.PrimaryColor = team.PrimaryColor;

        if (logoFile != null && logoFile.Length > 0)
        {
            var newLogoPath = await SaveTeamLogoAsync(logoFile);
            if (string.IsNullOrWhiteSpace(newLogoPath))
            {
                ModelState.AddModelError("", "Only JPG, JPEG, PNG, and WEBP files up to 2 MB are allowed.");
                return View(team);
            }

            DeleteTeamLogoFile(existingTeam.LogoPath);
            existingTeam.LogoPath = newLogoPath;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Team updated successfully.";
        return RedirectToAction(nameof(Teams));
    }

    // =========================
    // EDIT ALL PLAYERS OF TEAM
    // =========================
    [HttpGet]
    public async Task<IActionResult> EditTeamPlayers(int id)
    {
        var team = await _db.Teams
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (team == null)
            return NotFound();

        return View(team);
    }





    


    // =========================
    // ✅ PASTE HERE
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTeamPlayers(int teamId, List<Player> players)
    {
        var team = await _db.Teams
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
            return NotFound();

        if (players == null || !players.Any())
        {
            TempData["Error"] = "No players found to update.";
            return RedirectToAction(nameof(EditTeamPlayers), new { id = teamId });
        }

        foreach (var postedPlayer in players)
        {
            if (string.IsNullOrWhiteSpace(postedPlayer.Name))
            {
                TempData["Error"] = "Player name cannot be empty.";
                return RedirectToAction(nameof(EditTeamPlayers), new { id = teamId });
            }

            var existingPlayer = team.Players.FirstOrDefault(p => p.Id == postedPlayer.Id);

            if (existingPlayer != null)
            {
                existingPlayer.Name = postedPlayer.Name.Trim();
                existingPlayer.Role = string.IsNullOrWhiteSpace(postedPlayer.Role)
                    ? "Batsman"
                    : postedPlayer.Role;

                // Status is a mutually-exclusive radio choice (Captain / ViceCaptain / Injured / None).
                // Mapped server-side so only one flag is ever true, regardless of what was posted.
                existingPlayer.IsCaptain = postedPlayer.Status == "Captain";
                existingPlayer.IsViceCaptain = postedPlayer.Status == "ViceCaptain";
                existingPlayer.IsInjured = postedPlayer.Status == "Injured";
            }
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "All players updated successfully.";
        return RedirectToAction(nameof(Teams));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeam(int id)
    {
        var team = await _db.Teams
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (team == null)
            return NotFound();

        bool hasMatches = await _db.Matches.AnyAsync(m =>
            m.TeamAId == id || m.TeamBId == id || m.WinnerTeamId == id);

        if (hasMatches)
        {
            TempData["Error"] = "This team is used in matches, so it cannot be deleted.";
            return RedirectToAction(nameof(Teams));
        }

        if (team.Players.Any())
        {
            _db.Players.RemoveRange(team.Players);
        }

        DeleteTeamLogoFile(team.LogoPath);

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Team deleted successfully.";
        return RedirectToAction(nameof(Teams));
    }

    public async Task<IActionResult> Players()
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        ViewBag.Teams = new SelectList(await _db.Teams.Where(t => t.TournamentId == activeId).ToListAsync(), "Id", "Name");

        var players = await _db.Players
            .Include(x => x.Team)
            .Where(x => x.Team!.TournamentId == activeId)
            .OrderBy(x => x.Team!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return View(players);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlayer(Player player, IFormFile? photoFile)
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        ModelState.Remove("Team");

        if (!ModelState.IsValid)
        {
            ViewBag.Teams = new SelectList(await _db.Teams.Where(t => t.TournamentId == activeId).ToListAsync(), "Id", "Name", player.TeamId);

            var players = await _db.Players
                .Include(x => x.Team)
                .Where(x => x.Team!.TournamentId == activeId)
                .OrderBy(x => x.Team!.Name)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return View("Players", players);
        }

        if (photoFile != null && photoFile.Length > 0)
        {
            var photoPath = await SavePlayerPhotoAsync(photoFile);
            if (string.IsNullOrWhiteSpace(photoPath))
            {
                ModelState.AddModelError("", "Only JPG, JPEG, PNG, and WEBP files up to 2 MB are allowed.");
                ViewBag.Teams = new SelectList(await _db.Teams.Where(t => t.TournamentId == activeId).ToListAsync(), "Id", "Name", player.TeamId);

                var players = await _db.Players
                    .Include(x => x.Team)
                    .Where(x => x.Team!.TournamentId == activeId)
                    .OrderBy(x => x.Team!.Name)
                    .ThenBy(x => x.Name)
                    .ToListAsync();

                return View("Players", players);
            }

            player.PhotoPath = photoPath;
        }

        _db.Players.Add(player);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Player created successfully.";
        return RedirectToAction(nameof(Players));
    }

    [HttpGet]
    public async Task<IActionResult> EditPlayer(int id)
    {
        var player = await _db.Players.FindAsync(id);

        if (player == null)
            return NotFound();

        ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(), "Id", "Name", player.TeamId);
        return View(player);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPlayer(Player player, IFormFile? photoFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(), "Id", "Name", player.TeamId);
            return View(player);
        }

        var existingPlayer = await _db.Players.FindAsync(player.Id);

        if (existingPlayer == null)
            return NotFound();

        existingPlayer.Name = player.Name;
        existingPlayer.Role = player.Role;
        existingPlayer.TeamId = player.TeamId;
        existingPlayer.IsCaptain = player.IsCaptain;
        existingPlayer.IsViceCaptain = player.IsViceCaptain;
        existingPlayer.IsInjured = player.IsInjured;

        if (photoFile != null && photoFile.Length > 0)
        {
            var newPhotoPath = await SavePlayerPhotoAsync(photoFile);
            if (string.IsNullOrWhiteSpace(newPhotoPath))
            {
                ModelState.AddModelError("", "Only JPG, JPEG, PNG, and WEBP files up to 2 MB are allowed.");
                ViewBag.Teams = new SelectList(await _db.Teams.ToListAsync(), "Id", "Name", player.TeamId);
                return View(player);
            }

            DeletePlayerPhotoFile(existingPlayer.PhotoPath);
            existingPlayer.PhotoPath = newPhotoPath;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Player updated successfully.";
        return RedirectToAction(nameof(Players));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlayer(int id)
    {
        var player = await _db.Players.FindAsync(id);

        if (player == null)
            return NotFound();

        bool usedInBatting = await _db.BattingScores.AnyAsync(x => x.PlayerId == id);
        bool usedInBowling = await _db.BowlingScores.AnyAsync(x => x.PlayerId == id);
        bool usedInBalls = await _db.BallByBalls.AnyAsync(x =>
            x.BatsmanId == id || x.BowlerId == id || x.OutBatsmanId == id || x.FielderId == id);

        if (usedInBatting || usedInBowling || usedInBalls)
        {
            TempData["Error"] = "This player is already used in scorecards, so cannot be deleted.";
            return RedirectToAction(nameof(Players));
        }

        _db.Players.Remove(player);
        await _db.SaveChangesAsync();

        DeletePlayerPhotoFile(player.PhotoPath);

        TempData["Success"] = "Player deleted successfully.";
        return RedirectToAction(nameof(Players));
    }

    [HttpGet]
    public async Task<IActionResult> CreateMatch()
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        await FillLists();

        return View(new Match
        {
            TournamentId = activeId,
            OversPerInnings = 10,
            MatchDate = DateTime.Today,
            Status = "Upcoming",
            CurrentInnings = 1,
            CurrentRuns = 0,
            CurrentWickets = 0,
            CurrentOver = 0,
            CurrentBall = 0,
            TossWinner = string.Empty,
            TossDecision = string.Empty,
            IsOverCompleted = false,
            IsInningsCompleted = false,
            IsMatchCompleted = false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMatch(Match match)
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        match.TournamentId = activeId;
        ModelState.Remove("Tournament");

        if (match.TeamAId == match.TeamBId)
        {
            ModelState.AddModelError(nameof(match.TeamBId), "Choose two different teams.");
        }

        if (match.OversPerInnings <= 0)
        {
            ModelState.AddModelError(nameof(match.OversPerInnings), "Overs must be greater than 0.");
        }

        if (!ModelState.IsValid)
        {
            await FillLists();
            return View(match);
        }

        match.Status = "Upcoming";
        match.CurrentInnings = 1;
        match.CurrentRuns = 0;
        match.CurrentWickets = 0;
        match.CurrentOver = 0;
        match.CurrentBall = 0;
        match.IsOverCompleted = false;
        match.IsInningsCompleted = false;
        match.IsMatchCompleted = false;

        match.TeamAScore = 0;
        match.TeamAWickets = 0;
        match.TeamAOvers = 0;

        match.TeamBScore = 0;
        match.TeamBWickets = 0;
        match.TeamBOvers = 0;

        match.Result = string.Empty;
        match.ResultText = null;
        match.WinnerTeamId = null;
        match.ManOfTheMatchPlayerId = null;

        match.TossWinner = string.Empty;
        match.TossDecision = string.Empty;

        match.StrikerId = null;
        match.NonStrikerId = null;
        match.CurrentBowlerId = null;
        match.Target = null;

        match.CurrentWideRuns = 0;
        match.CurrentNoBallRuns = 0;
        match.CurrentLegByeRuns = 0;

        _db.Matches.Add(match);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Match created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMatch(int id)
    {
        var match = await _db.Matches
            .Include(m => m.BattingScores)
            .Include(m => m.BowlingScores)
            .Include(m => m.BallByBalls)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
            return NotFound();

        if (match.BattingScores.Any())
            _db.BattingScores.RemoveRange(match.BattingScores);

        if (match.BowlingScores.Any())
            _db.BowlingScores.RemoveRange(match.BowlingScores);

        if (match.BallByBalls.Any())
            _db.BallByBalls.RemoveRange(match.BallByBalls);

        _db.Matches.Remove(match);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Match deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Toss(int id)
    {
        var match = await _db.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
            return NotFound();

        ViewBag.TeamOptions = new List<SelectListItem>
        {
            new SelectListItem { Value = match.TeamAId.ToString(), Text = match.TeamA?.Name ?? "Team A" },
            new SelectListItem { Value = match.TeamBId.ToString(), Text = match.TeamB?.Name ?? "Team B" }
        };

        ViewBag.DecisionOptions = new List<SelectListItem>
        {
            new SelectListItem { Value = "Bat", Text = "Bat" },
            new SelectListItem { Value = "Bowl", Text = "Bowl" }
        };

        return View(match);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toss(int id, int tossWinnerTeamId, string tossDecision)
    {
        var match = await _db.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
            return NotFound();

        if (tossWinnerTeamId != match.TeamAId && tossWinnerTeamId != match.TeamBId)
            ModelState.AddModelError("", "Please select a valid toss winner.");

        if (string.IsNullOrWhiteSpace(tossDecision))
            ModelState.AddModelError("", "Please select toss decision.");

        if (!ModelState.IsValid)
        {
            ViewBag.TeamOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = match.TeamAId.ToString(), Text = match.TeamA?.Name ?? "Team A" },
                new SelectListItem { Value = match.TeamBId.ToString(), Text = match.TeamB?.Name ?? "Team B" }
            };

            ViewBag.DecisionOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Bat", Text = "Bat" },
                new SelectListItem { Value = "Bowl", Text = "Bowl" }
            };

            return View(match);
        }

        match.TossWinner = tossWinnerTeamId == match.TeamAId
            ? (match.TeamA?.Name ?? string.Empty)
            : (match.TeamB?.Name ?? string.Empty);

        match.TossDecision = tossDecision;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Toss saved successfully.";
        return RedirectToAction(nameof(StartMatch), new { id = match.Id });
    }

    [HttpGet]
    public async Task<IActionResult> StartMatch(int id)
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        var match = await _db.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(match.TossWinner) || string.IsNullOrWhiteSpace(match.TossDecision))
        {
            TempData["Error"] = "Please complete toss first.";
            return RedirectToAction(nameof(Toss), new { id });
        }

        int battingTeamId = GetFirstInningsBattingTeamId(match);
        int bowlingTeamId = battingTeamId == match.TeamAId ? match.TeamBId : match.TeamAId;

        var battingPlayers = await _db.Players
            .Where(p => p.TeamId == battingTeamId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var bowlingPlayers = await _db.Players
            .Where(p => p.TeamId == bowlingTeamId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.StrikerList = new SelectList(battingPlayers, "Id", "Name");
        ViewBag.NonStrikerList = new SelectList(battingPlayers, "Id", "Name");
        ViewBag.BowlerList = new SelectList(bowlingPlayers, "Id", "Name");

        return View(match);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartMatch(int id, int strikerId, int nonStrikerId, int bowlerId)
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        var match = await _db.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
            return NotFound();

        if (strikerId == nonStrikerId)
        {
            TempData["Error"] = "Striker and Non-Striker must be different.";
            return RedirectToAction(nameof(StartMatch), new { id });
        }

        match.StrikerId = strikerId;
        match.NonStrikerId = nonStrikerId;
        match.CurrentBowlerId = bowlerId;

        match.Status = "Live";
        match.IsMatchCompleted = false;
        match.CurrentInnings = 1;
        match.CurrentRuns = 0;
        match.CurrentWickets = 0;
        match.CurrentOver = 0;
        match.CurrentBall = 0;
        match.IsOverCompleted = false;
        match.IsInningsCompleted = false;

        match.CurrentWideRuns = 0;
        match.CurrentNoBallRuns = 0;
        match.CurrentLegByeRuns = 0;

        match.PartnershipRunsStart = 0;
        match.PartnershipBallsStart = 0;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Match started successfully.";
        return RedirectToAction(nameof(LiveScoring), new { id = match.Id });
    }

    [HttpGet]
    public async Task<IActionResult> NextInnings(int id)
    {
        var match = await _db.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
            return NotFound();

        if (!match.IsInningsCompleted || match.CurrentInnings != 1)
        {
            TempData["Error"] = "Next innings cannot be started now.";
            return RedirectToAction(nameof(LiveScoring), new { id });
        }

        int battingTeamId = GetSecondInningsBattingTeamId(match);
        int bowlingTeamId = battingTeamId == match.TeamAId ? match.TeamBId : match.TeamAId;

        var battingPlayers = await _db.Players
            .Where(p => p.TeamId == battingTeamId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var bowlingPlayers = await _db.Players
            .Where(p => p.TeamId == bowlingTeamId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.StrikerList = new SelectList(battingPlayers, "Id", "Name");
        ViewBag.NonStrikerList = new SelectList(battingPlayers, "Id", "Name");
        ViewBag.BowlerList = new SelectList(bowlingPlayers, "Id", "Name");

        return View(match);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartNextInnings(int matchId, int strikerId, int nonStrikerId, int bowlerId)
    {
        var match = await _db.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null)
            return NotFound();

        if (!match.IsInningsCompleted || match.CurrentInnings != 1)
        {
            TempData["Error"] = "Next innings cannot be started now.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (strikerId == nonStrikerId)
        {
            TempData["Error"] = "Striker and Non-Striker must be different.";
            return RedirectToAction(nameof(NextInnings), new { id = matchId });
        }

        int firstInningsScore = GetFirstInningsBattingTeamId(match) == match.TeamAId
            ? match.TeamAScore
            : match.TeamBScore;

        match.Target = firstInningsScore + 1;
        match.CurrentInnings = 2;
        match.CurrentRuns = 0;
        match.CurrentWickets = 0;
        match.CurrentOver = 0;
        match.CurrentBall = 0;

        match.CurrentWideRuns = 0;
        match.CurrentNoBallRuns = 0;
        match.CurrentLegByeRuns = 0;

        match.PartnershipRunsStart = 0;
        match.PartnershipBallsStart = 0;

        match.StrikerId = strikerId;
        match.NonStrikerId = nonStrikerId;
        match.CurrentBowlerId = bowlerId;

        match.IsOverCompleted = false;
        match.IsInningsCompleted = false;
        match.IsMatchCompleted = false;
        match.Status = "Live";

        await _db.SaveChangesAsync();

        TempData["Success"] = "Second innings started successfully.";
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpGet]
    public async Task<IActionResult> LiveScoring(int id)
    {
        var activeId = GetActiveTournamentId(out var redirect);
        if (redirect != null) return redirect;

        var match = await _db.Matches
            .Include(m => m.TeamA).ThenInclude(t => t.Players)
            .Include(m => m.TeamB).ThenInclude(t => t.Players)
            .Include(m => m.Striker)
            .Include(m => m.NonStriker)
            .Include(m => m.CurrentBowler)
            .Include(m => m.WinnerTeam)
            .Include(m => m.BallByBalls).ThenInclude(b => b.Bowler)
            .Include(m => m.BallByBalls).ThenInclude(b => b.Batsman)
            .Include(m => m.BallByBalls).ThenInclude(b => b.OutBatsman)
            .Include(m => m.BallByBalls).ThenInclude(b => b.Fielder)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
            return NotFound();

        return View(match);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRun(int matchId, int runs)
    {
        var match = await GetLiveMatch(matchId);
        if (match == null) return NotFound();

        if (!match.StrikerId.HasValue || !match.NonStrikerId.HasValue)
        {
            TempData["Error"] = "Please select next batsman first.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsInningsCompleted)
        {
            TempData["Error"] = match.CurrentInnings == 1 ? "Innings completed. Start next innings." : "Match completed.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsOverCompleted)
        {
            TempData["Error"] = "Over completed. Please select next bowler.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        match.CurrentRuns += runs;

        RecordBall(match, runs, 0, false, false, false, false, false, null, null, null, null);
        AddLegalBall(match);

        if (runs == 1 || runs == 3 || runs == 5)
            SwapStrike(match);

        UpdateInningsScore(match);
        CheckSecondInningsCompletion(match);

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDotBall(int matchId)
    {
        var match = await GetLiveMatch(matchId);
        if (match == null) return NotFound();

        if (!match.StrikerId.HasValue || !match.NonStrikerId.HasValue)
        {
            TempData["Error"] = "Please select next batsman first.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsInningsCompleted)
        {
            TempData["Error"] = match.CurrentInnings == 1 ? "Innings completed. Start next innings." : "Match completed.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsOverCompleted)
        {
            TempData["Error"] = "Over completed. Please select next bowler.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        RecordBall(match, 0, 0, false, false, false, false, false, null, null, null, null);
        AddLegalBall(match);

        UpdateInningsScore(match);
        CheckSecondInningsCompletion(match);

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWide(int matchId, int extraRuns)
    {
        var match = await GetLiveMatch(matchId);
        if (match == null) return NotFound();

        if (!match.StrikerId.HasValue || !match.NonStrikerId.HasValue)
        {
            TempData["Error"] = "Please select next batsman first.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsInningsCompleted)
        {
            TempData["Error"] = match.CurrentInnings == 1 ? "Innings completed. Start next innings." : "Match completed.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsOverCompleted)
        {
            TempData["Error"] = "Over completed. Please select next bowler.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        int totalRuns = 1 + extraRuns;

        match.CurrentRuns += totalRuns;
        match.CurrentWideRuns += totalRuns;

        RecordBall(match, 0, totalRuns, true, false, false, false, false, null, null, null, null);

        if (totalRuns % 2 != 0)
            SwapStrike(match);

        UpdateInningsScore(match);
        CheckSecondInningsCompletion(match);

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNoBall(int matchId, int batRuns)
    {
        var match = await GetLiveMatch(matchId);
        if (match == null) return NotFound();

        if (!match.StrikerId.HasValue || !match.NonStrikerId.HasValue)
        {
            TempData["Error"] = "Please select next batsman first.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsInningsCompleted)
        {
            TempData["Error"] = match.CurrentInnings == 1 ? "Innings completed. Start next innings." : "Match completed.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsOverCompleted)
        {
            TempData["Error"] = "Over completed. Please select next bowler.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        int totalRuns = 1 + batRuns;

        match.CurrentRuns += totalRuns;
        match.CurrentNoBallRuns += 1;

        RecordBall(match, batRuns, 1, false, true, false, false, false, null, null, null, null);

        if (batRuns % 2 != 0)
            SwapStrike(match);

        UpdateInningsScore(match);
        CheckSecondInningsCompletion(match);

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLegBye(int matchId, int runs)
    {
        var match = await GetLiveMatch(matchId);
        if (match == null) return NotFound();

        if (!match.StrikerId.HasValue || !match.NonStrikerId.HasValue)
        {
            TempData["Error"] = "Please select next batsman first.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsInningsCompleted)
        {
            TempData["Error"] = match.CurrentInnings == 1 ? "Innings completed. Start next innings." : "Match completed.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsOverCompleted)
        {
            TempData["Error"] = "Over completed. Please select next bowler.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        match.CurrentRuns += runs;
        match.CurrentLegByeRuns += runs;

        RecordBall(match, 0, runs, false, false, true, false, false, null, null, null, null);
        AddLegalBall(match);

        if (runs == 1 || runs == 3 || runs == 5)
            SwapStrike(match);

        UpdateInningsScore(match);
        CheckSecondInningsCompletion(match);

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBye(int matchId, int runs)
    {
        var match = await GetLiveMatch(matchId);
        if (match == null) return NotFound();

        if (!match.StrikerId.HasValue || !match.NonStrikerId.HasValue)
        {
            TempData["Error"] = "Please select next batsman first.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsInningsCompleted)
        {
            TempData["Error"] = match.CurrentInnings == 1 ? "Innings completed. Start next innings." : "Match completed.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsOverCompleted)
        {
            TempData["Error"] = "Over completed. Please select next bowler.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        match.CurrentRuns += runs;

        RecordBall(match, 0, runs, false, false, false, true, false, null, null, null, null);
        AddLegalBall(match);

        if (runs == 1 || runs == 3 || runs == 5)
            SwapStrike(match);

        UpdateInningsScore(match);
        CheckSecondInningsCompletion(match);

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpGet]
    public IActionResult AddWicket(int matchId)
    {
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWicket(int matchId, int outBatsmanId, string wicketType, int? fielderId)
    {
        var match = await GetLiveMatch(matchId);
        if (match == null)
            return NotFound();

        if (!match.StrikerId.HasValue || !match.NonStrikerId.HasValue)
        {
            TempData["Error"] = "Please select next batsman first.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsInningsCompleted)
        {
            TempData["Error"] = match.CurrentInnings == 1
                ? "Innings completed. Start next innings."
                : "Match completed.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsOverCompleted)
        {
            TempData["Error"] = "Over completed. Please select next bowler.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (string.IsNullOrWhiteSpace(wicketType))
        {
            TempData["Error"] = "Please select wicket type.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        bool needsFielder = WicketNeedsFielder(wicketType);

        if (needsFielder && !fielderId.HasValue)
        {
            TempData["Error"] = "Please select fielder.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        bool strikerOut = match.StrikerId == outBatsmanId;
        bool nonStrikerOut = match.NonStrikerId == outBatsmanId;

        if (!strikerOut && !nonStrikerOut)
        {
            TempData["Error"] = "Out batsman must be striker or non-striker.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        match.CurrentWickets += 1;

        string wicketText = await BuildWicketText(match, outBatsmanId, wicketType, fielderId);

        RecordBall(
            match: match,
            runs: 0,
            extras: 0,
            isWide: false,
            isNoBall: false,
            isLegBye: false,
            isBye: false,
            isWicket: true,
            wicketType: wicketType,
            outBatsmanId: outBatsmanId,
            fielderId: needsFielder ? fielderId : null,
            wicketText: wicketText);

        AddLegalBall(match);

        // New pair starts a fresh partnership from this exact point
        match.PartnershipRunsStart = match.CurrentRuns;
        match.PartnershipBallsStart = match.LegalBallsBowled;

        if (strikerOut)
            match.StrikerId = null;
        else if (nonStrikerOut)
            match.NonStrikerId = null;

        UpdateInningsScore(match);
        CheckAllOutCompletion(match);
        CheckSecondInningsCompletion(match);

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);

        TempData["Success"] = "Wicket added successfully. Please select next batsman.";
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectNextBatsman(int matchId, int nextBatsmanId)
    {
        var match = await GetLiveMatch(matchId);
        if (match == null)
            return NotFound();

        if (match.IsInningsCompleted || match.Status == "Completed")
        {
            TempData["Error"] = "Cannot select next batsman now.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        int battingTeamId = GetBattingTeamId(match);

        bool validPlayer = await _db.Players.AnyAsync(p =>
            p.Id == nextBatsmanId &&
            p.TeamId == battingTeamId &&
            p.Id != match.StrikerId &&
            p.Id != match.NonStrikerId);

        if (!validPlayer)
        {
            TempData["Error"] = "Please select a valid next batsman.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.StrikerId == null)
            match.StrikerId = nextBatsmanId;
        else if (match.NonStrikerId == null)
            match.NonStrikerId = nextBatsmanId;
        else
        {
            TempData["Error"] = "Both batsmen are already selected.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);

        TempData["Success"] = "Next batsman selected successfully.";
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStrike(int matchId)
    {
        var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null)
            return NotFound();

        if (!match.StrikerId.HasValue || !match.NonStrikerId.HasValue)
        {
            TempData["Error"] = "Please select next batsman first.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (match.IsInningsCompleted)
        {
            TempData["Error"] = "Cannot change strike after innings completion.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        SwapStrike(match);
        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);

        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NextOver(int matchId, int bowlerId)
    {
        var match = await GetLiveMatch(matchId);
        if (match == null)
            return NotFound();

        if (match.IsInningsCompleted)
        {
            TempData["Error"] = "Innings completed. Start next innings.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        if (!match.IsOverCompleted)
        {
            TempData["Error"] = "Current over is not completed yet.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        // Standard cricket rule: the bowler who just finished an over cannot bowl the next one.
        if (bowlerId == match.CurrentBowlerId)
        {
            TempData["Error"] = "This bowler just finished an over and cannot bowl consecutive overs.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        // Spell limit: a bowler can bowl at most Overs/5 overs (rounded up), same rule CricHeroes/T20 uses.
        int maxOversPerBowler = (int)Math.Ceiling(match.OversPerInnings / 5.0);
        int bowlerLegalBallsSoFar = await _db.BallByBalls
            .Where(b => b.MatchId == matchId
                     && b.InningsNumber == match.CurrentInnings
                     && b.BowlerId == bowlerId
                     && b.IsLegalBall)
            .CountAsync();
        int bowlerOversSoFar = bowlerLegalBallsSoFar / 6;

        if (bowlerOversSoFar >= maxOversPerBowler)
        {
            TempData["Error"] = $"This bowler has already bowled the maximum {maxOversPerBowler} over(s) allowed per bowler.";
            return RedirectToAction(nameof(LiveScoring), new { id = matchId });
        }

        match.CurrentBowlerId = bowlerId;
        match.IsOverCompleted = false;

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);

        TempData["Success"] = "Next over bowler selected.";
        return RedirectToAction(nameof(LiveScoring), new { id = matchId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndMatch(int matchId)
    {
        var match = await _db.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null)
            return NotFound();

        match.IsOverCompleted = false;
        match.IsInningsCompleted = true;
        match.IsMatchCompleted = true;

        CompleteMatchResult(match);

        await _db.SaveChangesAsync();
        await NotifyMatchUpdated(matchId);

        TempData["Success"] = "Match ended successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AddBatting(int matchId)
    {
        await FillLists(matchId);
        ViewBag.MatchId = matchId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBatting(BattingScore score)
    {
        if (!ModelState.IsValid)
        {
            await FillLists(score.MatchId);
            ViewBag.MatchId = score.MatchId;
            return View(score);
        }

        _db.BattingScores.Add(score);
        await _db.SaveChangesAsync();

        return RedirectToAction("MatchScorecard", "Home", new { id = score.MatchId });
    }

    [HttpGet]
    public async Task<IActionResult> AddBowling(int matchId)
    {
        await FillLists(matchId);
        ViewBag.MatchId = matchId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBowling(BowlingScore score)
    {
        if (!ModelState.IsValid)
        {
            await FillLists(score.MatchId);
            ViewBag.MatchId = score.MatchId;
            return View(score);
        }

        _db.BowlingScores.Add(score);
        await _db.SaveChangesAsync();

        return RedirectToAction("MatchScorecard", "Home", new { id = score.MatchId });
    }

    [HttpGet]
    public async Task<IActionResult> ViewScorecard(int id)
    {
        var match = await _db.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
        {
            TempData["Error"] = "Match not found.";
            return RedirectToAction(nameof(Index));
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

        var vm = new ScorecardViewModel
        {
            Match = match,
            Innings1 = BuildInningsScorecard(
                inningsNumber: 1,
                battingTeamId: innings1BattingTeamId,
                battingTeamName: innings1BattingTeamId == match.TeamAId ? (match.TeamA?.Name ?? "Team A") : (match.TeamB?.Name ?? "Team B"),
                balls: innings1Balls,
                battingPlayers: innings1BattingTeamId == match.TeamAId ? teamAPlayers : teamBPlayers,
                bowlingPlayers: innings1BattingTeamId == match.TeamAId ? teamBPlayers : teamAPlayers
            ),
            Innings2 = BuildInningsScorecard(
                inningsNumber: 2,
                battingTeamId: innings2BattingTeamId,
                battingTeamName: innings2BattingTeamId == match.TeamAId ? (match.TeamA?.Name ?? "Team A") : (match.TeamB?.Name ?? "Team B"),
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

        return View(vm);
    }

    private async Task FillLists(int? matchId = null)
    {
        int? activeId = null;
        if (Request.Cookies.TryGetValue("ActiveTournamentId", out var val) && int.TryParse(val, out var id))
        {
            activeId = id;
        }

        var teamsQuery = _db.Teams.AsQueryable();
        var playersQuery = _db.Players.AsQueryable();

        if (activeId.HasValue)
        {
            teamsQuery = teamsQuery.Where(t => t.TournamentId == activeId);
            playersQuery = playersQuery.Where(p => p.Team!.TournamentId == activeId);
        }

        var teams = await teamsQuery.ToListAsync();
        var players = await playersQuery.ToListAsync();

        ViewBag.Teams = new SelectList(teams, "Id", "Name");
        ViewBag.Players = new SelectList(players, "Id", "Name");

        if (matchId.HasValue)
        {
            var match = await _db.Matches.FirstOrDefaultAsync(x => x.Id == matchId.Value);

            if (match != null)
            {
                var matchPlayers = players
                    .Where(p => p.TeamId == match.TeamAId || p.TeamId == match.TeamBId)
                    .ToList();

                var matchTeams = teams
                    .Where(t => t.Id == match.TeamAId || t.Id == match.TeamBId)
                    .ToList();

                ViewBag.Players = new SelectList(matchPlayers, "Id", "Name");
                ViewBag.Teams = new SelectList(matchTeams, "Id", "Name");
            }
        }
    }

    private async Task<Match?> GetLiveMatch(int matchId)
    {
        return await _db.Matches
            .Include(m => m.TeamA).ThenInclude(t => t.Players)
            .Include(m => m.TeamB).ThenInclude(t => t.Players)
            .Include(m => m.Striker)
            .Include(m => m.NonStriker)
            .Include(m => m.CurrentBowler)
            .Include(m => m.WinnerTeam)
            .Include(m => m.BallByBalls)
            .FirstOrDefaultAsync(m => m.Id == matchId);
    }

    private void RecordBall(
        Match match,
        int runs,
        int extras,
        bool isWide,
        bool isNoBall,
        bool isLegBye,
        bool isBye,
        bool isWicket,
        string? wicketType,
        int? outBatsmanId,
        int? fielderId,
        string? wicketText)
    {
        bool isLegalBall = !isWide && !isNoBall;
        int overNumber = match.CurrentOver;
        int ballNumber = match.CurrentBall + 1;

        string ballText = isWicket
            ? (wicketText ?? "W")
            : GetBallText(runs, extras, isWide, isNoBall, isLegBye, isBye, isWicket);

        var ball = new BallByBall
        {
            MatchId = match.Id,
            InningsNumber = match.CurrentInnings,
            OverNumber = overNumber,
            BallNumber = ballNumber,
            BowlerId = match.CurrentBowlerId,
            BatsmanId = match.StrikerId,
            Runs = runs,
            Extras = extras,
            IsWide = isWide,
            IsNoBall = isNoBall,
            IsLegBye = isLegBye,
            IsBye = isBye,
            IsWicket = isWicket,
            WicketType = wicketType,
            IsLegalBall = isLegalBall,
            BallText = ballText,
            OutBatsmanId = outBatsmanId,
            FielderId = fielderId
        };

        _db.BallByBalls.Add(ball);
    }

    private string GetBallText(
        int runs,
        int extras,
        bool isWide,
        bool isNoBall,
        bool isLegBye,
        bool isBye,
        bool isWicket)
    {
        if (isWicket) return "W";
        if (isWide) return extras == 1 ? "Wd" : $"Wd{extras}";
        if (isNoBall) return runs == 0 ? "Nb" : $"Nb+{runs}";
        if (isLegBye) return $"LB{extras}";
        if (isBye) return $"B{extras}";
        return runs.ToString();
    }

    private string GetBallTextFromExistingBall(BallByBall ball)
    {
        if (!string.IsNullOrWhiteSpace(ball.BallText))
            return ball.BallText;

        return GetBallText(
            ball.Runs,
            ball.Extras,
            ball.IsWide,
            ball.IsNoBall,
            ball.IsLegBye,
            ball.IsBye,
            ball.IsWicket);
    }

    private void SwapStrike(Match match)
    {
        int? temp = match.StrikerId;
        match.StrikerId = match.NonStrikerId;
        match.NonStrikerId = temp;
    }

    private void SetTargetAfterFirstInnings(Match match)
    {
        if (match.CurrentInnings == 1 && match.IsInningsCompleted)
        {
            match.Target = match.CurrentRuns + 1;
        }
    }

    private void AddLegalBall(Match match)
    {
        match.CurrentBall += 1;

        if (match.CurrentBall >= 6)
        {
            match.CurrentOver += 1;
            match.CurrentBall = 0;

            if (match.StrikerId.HasValue && match.NonStrikerId.HasValue)
                SwapStrike(match);

            if (match.CurrentOver >= match.OversPerInnings)
            {
                match.IsOverCompleted = false;
                match.IsInningsCompleted = true;

                if (match.CurrentInnings == 1)
                {
                    match.Status = "Innings Break";
                    SetTargetAfterFirstInnings(match);
                }
                else
                {
                    CompleteMatchResult(match);
                }
            }
            else
            {
                match.IsOverCompleted = true;
            }
        }
    }

    private void UpdateInningsScore(Match match)
    {
        if (GetBattingTeamId(match) == match.TeamAId)
        {
            match.TeamAScore = match.CurrentRuns;
            match.TeamAWickets = match.CurrentWickets;
            match.TeamAOvers = ConvertOvers(match.CurrentOver, match.CurrentBall);
        }
        else
        {
            match.TeamBScore = match.CurrentRuns;
            match.TeamBWickets = match.CurrentWickets;
            match.TeamBOvers = ConvertOvers(match.CurrentOver, match.CurrentBall);
        }
    }

    private void CheckAllOutCompletion(Match match)
    {
        if (match.CurrentWickets < 10)
            return;

        match.IsOverCompleted = false;
        match.IsInningsCompleted = true;

        if (match.CurrentInnings == 1)
        {
            match.Status = "Innings Break";
            SetTargetAfterFirstInnings(match);
        }
        else
        {
            CompleteMatchResult(match);
        }
    }

    private void CheckSecondInningsCompletion(Match match)
    {
        if (match.CurrentInnings != 2)
            return;

        if (match.Target.HasValue && match.CurrentRuns >= match.Target.Value)
        {
            match.IsOverCompleted = false;
            match.IsInningsCompleted = true;
            CompleteMatchResult(match);
        }
    }

    private void CompleteMatchResult(Match match)
    {
        match.Status = "Completed";
        match.IsOverCompleted = false;
        match.IsInningsCompleted = true;
        match.IsMatchCompleted = true;

        bool teamABattedFirst = DidTeamABatFirst(match);
        bool teamABattedSecond = !teamABattedFirst;
        bool teamBBattedSecond = teamABattedFirst;

        if (match.TeamAScore == match.TeamBScore)
        {
            match.WinnerTeamId = null;
            match.Result = "Match Draw";
            match.ResultText = "Match tied";
            return;
        }

        if (match.TeamAScore > match.TeamBScore)
        {
            match.WinnerTeamId = match.TeamAId;
            match.Result = $"{match.TeamA?.Name} won";

            if (teamABattedSecond)
            {
                int wicketsLeft = 10 - match.TeamAWickets;
                match.ResultText = $"{match.TeamA?.Name} won by {wicketsLeft} wickets";
            }
            else
            {
                int runsDiff = match.TeamAScore - match.TeamBScore;
                match.ResultText = $"{match.TeamA?.Name} won by {runsDiff} runs";
            }
        }
        else
        {
            match.WinnerTeamId = match.TeamBId;
            match.Result = $"{match.TeamB?.Name} won";

            if (teamBBattedSecond)
            {
                int wicketsLeft = 10 - match.TeamBWickets;
                match.ResultText = $"{match.TeamB?.Name} won by {wicketsLeft} wickets";
            }
            else
            {
                int runsDiff = match.TeamBScore - match.TeamAScore;
                match.ResultText = $"{match.TeamB?.Name} won by {runsDiff} runs";
            }
        }

        var balls = _db.BallByBalls
            .Where(b => b.MatchId == match.Id)
            .ToList();

        var playerIds = balls
            .SelectMany(b => new int?[] { b.BatsmanId, b.BowlerId, b.FielderId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        int bestPlayerId = 0;
        int bestPoints = -1;

        foreach (var playerId in playerIds)
        {
            int points = CalculatePlayerPoints(balls, playerId);

            if (points > bestPoints)
            {
                bestPoints = points;
                bestPlayerId = playerId;
            }
        }

        if (bestPlayerId != 0)
        {
            match.ManOfTheMatchPlayerId = bestPlayerId;
        }
    }

    private decimal ConvertOvers(int over, int ball)
    {
        return Convert.ToDecimal($"{over}.{ball}");
    }

    private bool DidTeamABatFirst(Match match)
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

    private int GetBattingTeamId(Match match)
    {
        return match.CurrentInnings == 1
            ? GetFirstInningsBattingTeamId(match)
            : GetSecondInningsBattingTeamId(match);
    }

    private int GetFirstInningsBattingTeamId(Match match)
    {
        return DidTeamABatFirst(match) ? match.TeamAId : match.TeamBId;
    }

    private int GetSecondInningsBattingTeamId(Match match)
    {
        return DidTeamABatFirst(match) ? match.TeamBId : match.TeamAId;
    }

    private bool WicketNeedsFielder(string wicketType)
    {
        return string.Equals(wicketType, "Caught", StringComparison.OrdinalIgnoreCase)
            || string.Equals(wicketType, "Run Out", StringComparison.OrdinalIgnoreCase)
            || string.Equals(wicketType, "Stumped", StringComparison.OrdinalIgnoreCase)
            || string.Equals(wicketType, "Caught Behind", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> BuildWicketText(Match match, int outBatsmanId, string wicketType, int? fielderId)
    {
        var outBatsman = await _db.Players
            .Where(p => p.Id == outBatsmanId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync() ?? "Unknown Batsman";

        var bowler = match.CurrentBowler?.Name ?? await _db.Players
            .Where(p => p.Id == match.CurrentBowlerId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync() ?? "Unknown Bowler";

        var fielder = fielderId.HasValue
            ? await _db.Players
                .Where(p => p.Id == fielderId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync() ?? "Unknown Fielder"
            : string.Empty;

        return wicketType switch
        {
            "Bowled" => $"{outBatsman} b {bowler}",
            "LBW" => $"{outBatsman} lbw b {bowler}",
            "Caught" => $"{outBatsman} c {fielder} b {bowler}",
            "Caught Behind" => $"{outBatsman} c {fielder} b {bowler}",
            "Run Out" => $"{outBatsman} run out ({fielder})",
            "Stumped" => $"{outBatsman} st {fielder} b {bowler}",
            "Hit Wicket" => $"{outBatsman} hit wicket b {bowler}",
            _ => $"{outBatsman} out"
        };
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
            BattingTeamName = battingTeamName,
            Batting = new List<BattingRowViewModel>(),
            Bowling = new List<BowlingRowViewModel>(),
            FallOfWickets = new List<FallOfWicketViewModel>()
        };

        if (balls == null || !balls.Any())
        {
            vm.TotalRuns = 0;
            vm.Wickets = 0;
            vm.Overs = 0;
            vm.RunRate = 0;
            vm.Byes = 0;
            vm.LegByes = 0;
            vm.Wides = 0;
            vm.NoBalls = 0;
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
        var wickets = new List<FallOfWicketViewModel>();
        int runningScore = 0;

        foreach (var ball in balls.OrderBy(b => b.OverNumber).ThenBy(b => b.BallNumber))
        {
            runningScore += GetTeamRunsFromBall(ball);

            if (!ball.IsWicket)
                continue;

            wickets.Add(new FallOfWicketViewModel
            {
                PlayerId = ball.OutBatsmanId ?? ball.BatsmanId ?? 0,
                PlayerName = ball.OutBatsman?.Name ?? ball.Batsman?.Name ?? "Unknown Player",
                WicketText = !string.IsNullOrWhiteSpace(ball.BallText) ? ball.BallText : "out",
                TeamScoreAtFall = runningScore,
                OverText = $"{ball.OverNumber}.{ball.BallNumber}"
            });
        }

        return wickets;
    }

    private int GetTeamRunsFromBall(BallByBall ball)
    {
        if (ball == null)
            return 0;

        return (ball.Runs < 0 ? 0 : ball.Runs) + (ball.Extras < 0 ? 0 : ball.Extras);
    }

    private int CalculatePlayerPoints(List<BallByBall> balls, int playerId)
    {
        var battingBalls = balls.Where(b => b.BatsmanId == playerId).ToList();
        var bowlingBalls = balls.Where(b => b.BowlerId == playerId).ToList();
        var fieldingBalls = balls.Where(b => b.FielderId == playerId).ToList();

        int runs = battingBalls
            .Where(b => !b.IsBye && !b.IsLegBye)
            .Sum(b => b.Runs);

        int ballsFaced = battingBalls.Count(b => b.IsLegalBall);

        int wickets = bowlingBalls.Count(b => b.IsWicket);

        int catches = fieldingBalls.Count(b =>
            b.IsWicket && b.WicketType == "Caught");

        int runOuts = fieldingBalls.Count(b =>
            b.IsWicket && b.WicketType == "Run Out");

        double strikeRate = ballsFaced > 0 ? (runs * 100.0 / ballsFaced) : 0;

        int ballsBowled = bowlingBalls.Count(b => b.IsLegalBall);
        int runsGiven = bowlingBalls.Sum(b => b.Runs + b.Extras);
        double economy = ballsBowled > 0 ? (runsGiven * 6.0 / ballsBowled) : 0;

        int points =
            runs +
            (wickets * 25) +
            (catches * 10) +
            (runOuts * 10);

        if (strikeRate > 150) points += 10;
        if (economy > 0 && economy < 6) points += 10;

        return points;
    }

    private async Task<string?> SaveTeamLogoAsync(IFormFile logoFile)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            return null;

        if (logoFile.Length > 2 * 1024 * 1024)
            return null;

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "teams");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await logoFile.CopyToAsync(stream);
        }

        return $"/uploads/teams/{uniqueFileName}";
    }

    private async Task<string?> SavePlayerPhotoAsync(IFormFile photoFile)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(photoFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            return null;

        if (photoFile.Length > 2 * 1024 * 1024)
            return null;

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "players");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await photoFile.CopyToAsync(stream);
        }

        return $"/uploads/players/{uniqueFileName}";
    }

    private void DeletePlayerPhotoFile(string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
            return;

        var relativePath = photoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }

    private void DeleteTeamLogoFile(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
            return;

        var relativePath = logoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }

    [HttpGet]
    public IActionResult CreateTournament()
    {
        var email = User.Identity.Name;
        return View(new Tournament { AdminEmail = email ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTournament(Tournament tournament)
    {
        // Ignore validation errors for child lists or navigation properties if any
        ModelState.Remove("Teams");
        ModelState.Remove("Matches");
        if (!ModelState.IsValid)
            return View(tournament);

        tournament.AdminEmail = User.Identity.Name ?? string.Empty;
        _db.Tournaments.Add(tournament);
        await _db.SaveChangesAsync();

        Response.Cookies.Append("ActiveTournamentId", tournament.Id.ToString(), new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        TempData["Success"] = "Tournament created successfully!";
        return RedirectToAction("Index", "Admin");
    }

    [HttpGet]
    public async Task<IActionResult> Tournaments()
    {
        var email = User.Identity.Name;
        var list = await _db.Tournaments
            .Where(t => t.AdminEmail == email)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> SelectTournament(int id)
    {
        var email = User.Identity.Name;
        var tournament = await _db.Tournaments.FirstOrDefaultAsync(t => t.Id == id && t.AdminEmail == email);
        if (tournament == null)
        {
            TempData["Error"] = "Tournament not found or access denied.";
            return RedirectToAction("Tournaments");
        }

        Response.Cookies.Append("ActiveTournamentId", tournament.Id.ToString(), new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        return RedirectToAction("Index", "Admin");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTournament(int id)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(t => t.Id == id && t.AdminEmail == email);

        if (tournament == null)
        {
            TempData["Error"] = "Tournament not found or access denied.";
            return RedirectToAction(nameof(Tournaments));
        }

        try
        {
            // 1. Get Match IDs in this tournament
            var matchIds = await _db.Matches
                .Where(m => m.TournamentId == id)
                .Select(m => m.Id)
                .ToListAsync();

            if (matchIds.Any())
            {
                // Delete FallOfWickets
                var fow = _db.FallOfWickets.Where(f => matchIds.Contains(f.MatchId));
                _db.FallOfWickets.RemoveRange(fow);

                // Delete MatchBalls
                var mb = _db.MatchBalls.Where(b => matchIds.Contains(b.MatchId));
                _db.MatchBalls.RemoveRange(mb);

                // Delete LiveScores
                var ls = _db.LiveScores.Where(l => matchIds.Contains(l.MatchId));
                _db.LiveScores.RemoveRange(ls);

                // Delete BallByBalls
                var bbb = _db.BallByBalls.Where(b => matchIds.Contains(b.MatchId));
                _db.BallByBalls.RemoveRange(bbb);

                // Delete BattingScores
                var bs = _db.BattingScores.Where(b => matchIds.Contains(b.MatchId));
                _db.BattingScores.RemoveRange(bs);

                // Delete BowlingScores
                var bos = _db.BowlingScores.Where(b => matchIds.Contains(b.MatchId));
                _db.BowlingScores.RemoveRange(bos);

                // Delete Matches
                var matches = _db.Matches.Where(m => m.TournamentId == id);
                _db.Matches.RemoveRange(matches);
            }

            // 2. Get Team IDs in this tournament
            var teams = await _db.Teams
                .Where(t => t.TournamentId == id)
                .ToListAsync();

            if (teams.Any())
            {
                var teamIds = teams.Select(t => t.Id).ToList();

                // Delete Players
                var players = _db.Players.Where(p => teamIds.Contains(p.TeamId));
                _db.Players.RemoveRange(players);

                // Delete Teams
                _db.Teams.RemoveRange(teams);
            }

            // 3. Delete Tournament itself
            _db.Tournaments.Remove(tournament);

            // 4. Save changes
            await _db.SaveChangesAsync();

            // 5. Clean up cookie if it is the deleted tournament
            if (Request.Cookies.TryGetValue("ActiveTournamentId", out var cookieVal))
            {
                if (int.TryParse(cookieVal, out var activeId) && activeId == id)
                {
                    Response.Cookies.Delete("ActiveTournamentId");
                }
            }

            TempData["Success"] = "Tournament and all associated data deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting tournament: {ex.Message}";
        }

        return RedirectToAction(nameof(Tournaments));
    }

    public async Task<IActionResult> PlayersList()
    {
        var players = await _db.Players
            .Include(p => p.Team)
            .OrderBy(p => p.Team!.Name)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return View(players);
    }
}