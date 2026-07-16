using KplTournament.Web.Data;
using KplTournament.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace KplTournament.Web.Controllers
{
    public class LandingController : Controller
    {
        private readonly AppDbContext _db;

        public LandingController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var tournaments = await _db.Tournaments
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            ViewBag.TotalTeams = await _db.Teams.CountAsync(t => t.TournamentId != null);
            ViewBag.TotalMatches = await _db.Matches.CountAsync(m => m.TournamentId != null);
            ViewBag.TotalRuns = await _db.Matches.Where(m => m.TournamentId != null).AnyAsync()
                ? await _db.Matches.Where(m => m.TournamentId != null).SumAsync(m => m.TeamAScore + m.TeamBScore)
                : 0;
            ViewBag.TotalWickets = await _db.Matches.Where(m => m.TournamentId != null).AnyAsync()
                ? await _db.Matches.Where(m => m.TournamentId != null).SumAsync(m => m.TeamAWickets + m.TeamBWickets)
                : 0;

            ViewBag.LiveMatches = await _db.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.Tournament)
                .Where(m => m.Status == "Live" && m.TournamentId != null)
                .OrderByDescending(m => m.MatchDate)
                .ToListAsync();

            return View(tournaments);
        }

        [HttpPost]
        public IActionResult SelectTournament(int id)
        {
            Response.Cookies.Append("ActiveTournamentId", id.ToString(), new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = System.DateTimeOffset.UtcNow.AddDays(30)
            });

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}
