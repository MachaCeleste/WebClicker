using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Reflection;
using WebappServer;
using WebappServer.NetworkMessages;

namespace WebClicker;

public class Program
{
    public static void Main(string[] args)
    {
        InitDatabase(args.Length > 0 && args[0] == "--wipe");
        // TODO Add chat system

        //new BadwordHandler();
        LeaderboardHandler.UpdateLeaderboard(15);

        var builder = WebApplication.CreateBuilder(args);
        string jwtSecret = builder.Configuration["JwtSecret"] ?? "3AqqlBtEZvGJ9P6UwP8esMZLYpNjLCFa";
        AppServer.Singleton.SetSecret(jwtSecret);
        var app = builder.Build();
        ConfigureMiddleware(app);

        AppServer.Singleton.OnMessageReceived += OnMessageReceived;
        AppServer.Singleton.OnClientConnected += OnClientConnected;
        AppServer.Singleton.OnClientDisconnect += OnClientDisconnect;

        app.Run();
    }

    public static void OnMessageReceived(string userId, ServerMessage message)
    {
        MessageReceivedHandler.ProcessMessageReceived(userId, message);
    }

    public static void OnClientConnected(string userId)
    {
        Console.WriteLine($"User {userId} connected.");
        DatabaseHandler.Singleton.CreateUser(userId);
        ClientMethods.UpdateClientCounter(userId);
        ClientMethods.UpdateUserInfo(userId);
        ClientMethods.UpdateChat(userId);
    }

    public static void OnClientDisconnect(string userId)
    {
        // TODO idk should i even handle a disconnect?
    }

    private static void InitDatabase(bool shouldWipe)
    {
        FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        var _filePath = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData),
            fileInfo.CompanyName ?? "DefaultCompany",
            fileInfo.ProductName ?? "WebClicker");
        if (!Directory.Exists(_filePath))
            Directory.CreateDirectory(_filePath);
        new DatabaseHandler(_filePath, shouldWipe);
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseDefaultFiles();
        if (app.Environment.IsDevelopment())
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
                    ctx.Context.Response.Headers.Append("Expires", "-1");
                }
            });
        }
        else
        {
            app.UseStaticFiles();
        }
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2)
        });

        app.Map("/connect", async (HttpContext context) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await AppServer.Singleton.HandleNewConnection(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });

        app.MapGet("/auth/register", () =>
        {
            var tokens = AppServer.Singleton.GrantTokens();
            return Results.Ok(new { tokens.AccessToken, tokens.RefreshToken });
        });

        app.MapPost("/auth/refresh", (string refreshToken) =>
        {
            var userId = AppServer.Singleton.ValidateToken(refreshToken);
            if (userId == null) return Results.Unauthorized();
            var tokens = AppServer.Singleton.GrantTokens(userId);
            return Results.Ok(new { tokens.AccessToken, tokens.RefreshToken });
        });
    }
}
