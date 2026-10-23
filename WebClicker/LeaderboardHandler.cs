using WebappServer;
using WebappServer.NetworkMessages;
using WebClicker.enums;

namespace WebClicker;

public class LeaderboardHandler
{
    public static async Task UpdateLeaderboard(int interval)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
        while (await timer.WaitForNextTickAsync())
        {
            var clientMessage = new ClientMessage((int)ClientOpCodes.UpdateLeaderboard);
            List<string> leaderboard = DatabaseHandler.Singleton.GetLeaderboard();
            if (leaderboard.Count > 0)
            {
                foreach (var user in leaderboard)
                {
                    clientMessage.AddString(user);
                }
            }
            else
                clientMessage.AddString("No Users!");
            AppServer.Singleton.MessageAllClients(clientMessage);
        }
    }
}
