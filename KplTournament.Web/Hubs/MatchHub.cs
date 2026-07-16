using Microsoft.AspNetCore.SignalR;

namespace KplTournament.Web.Hubs
{
    /// <summary>
    /// Broadcasts live score updates to anyone viewing a match.
    /// Clients join a per-match group so updates only go to people watching that match.
    /// No server-side game logic lives here — the hub is purely a notification pipe;
    /// the existing AdminController scoring actions remain the single source of truth.
    /// </summary>
    public class MatchHub : Hub
    {
        public async Task JoinMatch(int matchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(matchId));
        }

        public async Task LeaveMatch(int matchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(matchId));
        }

        public static string GroupName(int matchId) => $"match-{matchId}";
    }
}
