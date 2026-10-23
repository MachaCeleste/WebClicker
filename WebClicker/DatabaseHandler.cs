using System.Text.Json;
using FoxDatabase;
using WebClicker.enums;
using WebClicker.structs;

namespace WebClicker;

internal class DatabaseHandler
{
    public static DatabaseHandler Singleton;

    private Database? db;
    private string dbPath;

    public DatabaseHandler(string path, bool shouldWipe)
    {
        this.dbPath = Path.Combine(path, "system.db");
        if (shouldWipe) ResetDatabase();
        db = new Database(dbPath);
        CreateDatabase();
        DatabaseHandler.Singleton = this;
    }

    private void CreateDatabase()
    {
        db?.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS Users (
                UserID TEXT PRIMARY KEY,
                Clicks INTEGER DEFAULT 0,
                Lifetime INTEGER DEFAULT 0,
                Nickname TEXT DEFAULT 'Anonymous',
                Upgrades TEXT,
                Theme INTEGER DEFAULT 0,
                DateJoined TEXT
            );

            CREATE TABLE IF NOT EXISTS Chat (
                MessageID INTEGER PRIMARY KEY AUTOINCREMENT,
                UserID TEXT,
                Date TEXT,
                Message TEXT
            );

            CREATE TRIGGER IF NOT EXISTS purge_old_messages
            AFTER INSERT ON Chat
            BEGIN
                DELETE FROM Chat
                WHERE MessageID NOT IN (
                    SELECT MessageID FROM Chat
                    ORDER BY MessageID DESC
                    LIMIT 15
                );
            END;
        ");
    }

    private void ResetDatabase()
    {
        if (db != null)
        {
            db?.Dispose();
            db = null;
        }

        try
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                if (File.Exists($"{dbPath}-shm")) File.Delete($"{dbPath}-shm");
                if (File.Exists($"{dbPath}-wal")) File.Delete($"{dbPath}-wal");
                Console.WriteLine("Database deleted!");
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO Error: {ex}");
        }
    }

    public void CreateUser(string userId)
    {
        db.ExecuteNonQuery("INSERT OR IGNORE INTO Users (UserID, DateJoined) VALUES ($userid, $date)",
            new Dictionary<string, object> { { "$userid", userId }, { "$date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") } });
    }

    public int GetUserClicks(string userId)
    {
        var row = db.ExecuteQuery("SELECT Clicks FROM Users WHERE UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId } })[0];
        int clicks = Convert.ToInt32(row["Clicks"]);
        return clicks;
    }

    public void AddUserClicks(string userId)
    {
        int clicks = 1;
        var upgrades = GetUserUpgrades(userId);

        Upgrade? multi = upgrades.FirstOrDefault(x => x.UpgradeType == UpgradeTypes.Multiplier);
        if (multi != null && multi.State == true) clicks += multi.Level;

        db.ExecuteNonQuery("UPDATE Users SET Clicks = Clicks + $clicks, Lifetime = Lifetime + $clicks WHERE UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId }, { "$clicks", clicks } });
    }

    public void RemoveUserClicks(string userId, int amount)
    {
        db.ExecuteNonQuery("UPDATE Users SET Clicks = Clicks - $amount WHERE UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId }, { "$amount", amount } });
    }

    public int GetUserLifetime(string userId)
    {
        var row = db.ExecuteQuery("SELECT Lifetime FROM Users Where UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId } })[0];
        int lifetime = Convert.ToInt32(row["Lifetime"]);
        return lifetime;
    }

    public string GetUserNickname(string userId)
    {
        var row = db.ExecuteQuery("SELECT Nickname FROM Users WHERE UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId } })[0];
        return row["Nickname"]?.ToString() ?? "Anonymous";
    }

    public void SetUserNickname(string userId, string nickName)
    {
        db.ExecuteNonQuery("UPDATE Users SET Nickname = $nick WHERE UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId }, { "$nick", nickName } });
    }

    public List<Upgrade> GetUserUpgrades(string userId)
    {
        var row = db.ExecuteQuery("SELECT Upgrades FROM Users WHERE UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId } })[0];
        var json = row["Upgrades"]?.ToString();
        return string.IsNullOrEmpty(json) ? new List<Upgrade>() : JsonSerializer.Deserialize<List<Upgrade>>(json) ?? new List<Upgrade>();
    }

    public Upgrade? GetUserUpgrade(string userId, UpgradeTypes upgradeType)
    {
        return GetUserUpgrades(userId).FirstOrDefault(x => x.UpgradeType == upgradeType);
    }

    public void SetUserUpgrades(string userId, List<Upgrade> upgrades)
    {
        string json = JsonSerializer.Serialize(upgrades);
        db.ExecuteNonQuery("UPDATE Users SET Upgrades = $upgrades WHERE UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId }, { "$upgrades", json } });
    }

    public int GetUserTheme(string userId)
    {
        var row = db.ExecuteQuery("SELECT Theme FROM Users WHERE UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId } })[0];
        int theme = Convert.ToInt32(row["Theme"]);
        return theme;
    }

    public void SetUserTheme(string userId, int theme)
    {
        db.ExecuteNonQuery("UPDATE Users SET Theme = $theme WHERE UserID = $userid",
            new Dictionary<string, object> { { "$userid", userId }, { "$theme", theme } });
    }

    public List<string> GetLeaderboard(int limit = 5)
    {
        var rows = db.ExecuteQuery("SELECT Nickname, UserID, Lifetime, Clicks FROM Users ORDER BY Clicks DESC LIMIT $limit",
            new Dictionary<string, object> { { "$limit", limit } });
        List<string> result = new List<string>();
        foreach (var row in rows)
        {
            result.Add($"{row["Nickname"]}|{row["UserID"]}|{row["Lifetime"]}|{row["Clicks"]}");
        }
        return result;
    }

    public void AddChatMessage(string userId, string message)
    {
        db.ExecuteNonQuery("INSERT INTO Chat (UserID, Date, Message) VALUES ($userid, $date, $msg)",
            new Dictionary<string, object> { { "$userid", userId }, { "$date", DateTime.UtcNow.ToString("yyyy/MM/dd - HH:mm:ss") }, { "$msg", message } });
    }

    public List<string> GetRecentMessages()
    {
        var rows = db.ExecuteQuery("SELECT * FROM Chat ORDER BY MessageID ASC");
        List<string> result = new List<string>();
        foreach (var row in rows)
        {
            string userId = row["UserID"].ToString();
            string nickname = GetUserNickname(userId);
            result.Add($"{nickname}|{userId}|{row["Date"]}|{row["Message"]}");
        }
        return result.Count > 0 ? result : new List<string>();
    }
}
