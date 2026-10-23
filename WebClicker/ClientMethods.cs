using System.Text.Json;
using WebappServer;
using WebappServer.NetworkMessages;
using WebClicker.enums;

namespace WebClicker;

public class ClientMethods
{
    public static void NotifyClient(string userId, string message)
    {
        var clientMessage = new ClientMessage((int)ClientOpCodes.NotifyClient);
        clientMessage.AddString(message);
        var client = AppServer.Singleton.GetClient(userId);
        if (client != null) client.SendMessage(clientMessage);
    }

    public static void UpdateClientCounter(string userId)
    {
        var count = DatabaseHandler.Singleton.GetUserClicks(userId);
        var clientMessage = new ClientMessage((int)ClientOpCodes.UpdateCounter);
        clientMessage.AddInt(count);
        var client = AppServer.Singleton.GetClient(userId);
        if (client != null) client.SendMessage(clientMessage);
    }

    public static void UpdateUserInfo(string userId)
    {
        var clientMessage = new ClientMessage((int)ClientOpCodes.UpdateUserInfo);
        string json = JsonSerializer.Serialize(DatabaseHandler.Singleton.GetUserUpgrades(userId));
        clientMessage.AddString(json);
        clientMessage.AddInt(DatabaseHandler.Singleton.GetUserTheme(userId));
        var client = AppServer.Singleton.GetClient(userId);
        client?.SendMessage(clientMessage);
    }

    public static void UpdateChat()
    {
        ClientMessage clientMessage = new ClientMessage((int)ClientOpCodes.UpdateChat);
        foreach (string line in DatabaseHandler.Singleton.GetRecentMessages()) clientMessage.AddString(line);
        AppServer.Singleton.MessageAllClients(clientMessage);
    }

    public static void UpdateChat(string userId)
    {
        ClientMessage clientMessage = new ClientMessage((int)ClientOpCodes.UpdateChat);
        foreach (string line in DatabaseHandler.Singleton.GetRecentMessages()) clientMessage.AddString(line);
        var client = AppServer.Singleton.GetClient(userId);
        client?.SendMessage(clientMessage);
    }
}
