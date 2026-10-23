using WebappServer.NetworkMessages;
using WebClicker.enums;
using WebClicker.structs;

namespace WebClicker;

public class UpgradeHandler
{
    public static void UpgradeRequested(string userId, ServerMessage message)
    {
        UpgradeTypes upgradeType = (UpgradeTypes)message.GetInt();
        List<Upgrade> upgrades = DatabaseHandler.Singleton.GetUserUpgrades(userId);
        int currentClicks = DatabaseHandler.Singleton.GetUserClicks(userId);
        int cost = 0;
        bool purchaced = false;
        switch (upgradeType)
        {
            case UpgradeTypes.Nickname:
                if (upgrades.FirstOrDefault(x => x.UpgradeType == UpgradeTypes.Nickname) == null)
                {
                    string nickName = message.GetString();
                    if (!Utils.IsNicknameValid(nickName, out string reason))
                    {
                        ClientMethods.NotifyClient(userId, reason);
                        return;
                    }
                    Upgrade nameUpgrade = new Upgrade(UpgradeTypes.Nickname);
                    cost = nameUpgrade.Cost;
                    if (currentClicks >= cost)
                    {
                        DatabaseHandler.Singleton.SetUserNickname(userId, nickName);
                        upgrades.Add(nameUpgrade);
                        purchaced = true;
                    }
                }
                break;
            default:
                Upgrade? upgrade = upgrades.FirstOrDefault(x => x.UpgradeType == upgradeType);
                bool add = false;
                if (upgrade == null)
                {
                    upgrade = new Upgrade(upgradeType);
                    add = true;
                }
                if (upgrade.Level > 0 && !upgrade.Multilevel)
                {
                    ClientMethods.NotifyClient(userId, $"Cannot upgrade {upgradeType}, it is not upgradable!");
                    return;
                }
                else if (upgrade.Cap > 0 && upgrade.Level >= upgrade.Cap)
                {
                    ClientMethods.NotifyClient(userId, $"Cannot upgrade {upgradeType}, it is at level cap!");
                    return;
                }
                cost = upgrade.Cost;
                if (currentClicks >= cost)
                {
                    upgrade.Level++;
                    if (add)
                        upgrades.Add(upgrade);
                    purchaced = true;
                }
                break;
        }
        if (purchaced)
        {
            DatabaseHandler.Singleton.RemoveUserClicks(userId, cost);
            DatabaseHandler.Singleton.SetUserUpgrades(userId, upgrades);
            //Console.WriteLine($"User {userId} purchased {upgradeType}"); // Debugging
            ClientMethods.UpdateUserInfo(userId);
            ClientMethods.UpdateClientCounter(userId);
        }
        else
            ClientMethods.NotifyClient(userId, $"Unable to purchase upgrade {upgradeType}");
    }
}
