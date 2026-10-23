using WebappServer;
using WebappServer.NetworkMessages;
using WebClicker.enums;
using WebClicker.structs;

namespace WebClicker;

public class MessageReceivedHandler
{
    public static void ProcessMessageReceived(string userId, ServerMessage message)
    {
        switch ((ServerOpCodes)message.ID)
        {
            case ServerOpCodes.UserClicked:
                ClickReceivedHandlerRPC(userId, message);
                break;
            case ServerOpCodes.UserUpgraded:
                UpgradeReceivedHandlerRPC(userId, message);
                break;
            case ServerOpCodes.UserToggledUpgrade:
                ToggleUpgradeReceivedRPC(userId, message);
                break;
            case ServerOpCodes.UserSetTheme:
                SetThemeReceivedRPC(userId, message);
                break;
            case ServerOpCodes.ChatMessage:
                ChatMessageReceivedRPC(userId, message);
                break;
        }
    }

    public static void ClickReceivedHandlerRPC(string userId, ServerMessage message)
    {
        //Console.WriteLine($"User {userId} clicked"); // Debugging
        DatabaseHandler.Singleton.AddUserClicks(userId);
        ClientMethods.UpdateClientCounter(userId);
    }

    public static void UpgradeReceivedHandlerRPC(string userId, ServerMessage message)
    {
        UpgradeHandler.UpgradeRequested(userId, message);
    }

    public static void ToggleUpgradeReceivedRPC(string userId, ServerMessage message)
    {
        UpgradeTypes upgradeType = (UpgradeTypes)message.GetInt();
        List<Upgrade> upgrades = DatabaseHandler.Singleton.GetUserUpgrades(userId);
        Upgrade? upgrade = upgrades.FirstOrDefault(x => x.UpgradeType == upgradeType);
        if (upgrade != null && upgrade.Level > 0)
        {
            upgrade.State = !upgrade.State;
            DatabaseHandler.Singleton.SetUserUpgrades(userId, upgrades);
            ClientMethods.UpdateUserInfo(userId);
        }
    }

    public static void SetThemeReceivedRPC(string userId, ServerMessage message)
    {
        int themeNumber = message.GetInt();
        Upgrade? themeUpgrade = DatabaseHandler.Singleton.GetUserUpgrade(userId, UpgradeTypes.Theme);
        if (themeUpgrade != null && themeUpgrade.Level > 0 && themeUpgrade.Level >= themeNumber)
        {
            DatabaseHandler.Singleton.SetUserTheme(userId, themeNumber);
            ClientMethods.UpdateUserInfo(userId);
        }
    }

    public static void ChatMessageReceivedRPC(string userId, ServerMessage message)
    {
        string content = message.GetString();
        //Console.WriteLine($"{userId} {content}"); // Debugging
        if (string.IsNullOrEmpty(content)) return; // maybe add handling?
        // TODO add command handler
        DatabaseHandler.Singleton.AddChatMessage(userId, content);
        ClientMethods.UpdateChat();
    }
}
