using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using DibraServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

BotServer.Print("Made by discord: VivoDibra#1182");
BotServer.Print("Changes by: Kevinf100");
BotServer.Print("Starting...");
var wsOptions = new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(120) };
app.UseWebSockets(wsOptions);

var socketManager = new SocketManager();

async Task SendPing(ConcurrentDictionary<WebSocket, object?> channel)
{
    JObject jsonObj = new()
    {
        { "name", "Console" },
        { "type", "ping" }
    };
    List<Task> tasks = new();
    foreach (var socket in channel.Keys)
    {
        try
        {
            tasks.Add(socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonObj))), WebSocketMessageType.Text, true, CancellationToken.None));
        }
        catch (Exception e)
        {
            socket.Abort();
            BotServer.Print($"An error has happened while trying to ping.\n{e}");
        }
    }
    await Task.WhenAll(tasks);
}

async Task SendPingAll()
{
    List<Task> tasks = new();
    foreach (var channel in socketManager.connections.Values)
    {
        tasks.Add(SendPing(channel));
    }
    await Task.WhenAll(tasks);
}

async Task SendPingLoop()
{
    var tasks = new List<Task>();
    while (!socketManager.ServerLoop.IsCancellationRequested)
    {
        tasks.Add(Task.Delay(1000));
        tasks.Add(SendPingAll());
        await Task.WhenAll(tasks);
        tasks.Clear();
    }
    BotServer.Print("Shutting down server.");
}


app.Use(async (HttpContext context, Func<Task> next) =>
{
    try
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await SendToAll(webSocket);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }

        await next();
    }
    catch (Exception)
    {
        //silence...
    }

});

async Task SendToAll(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    string name = "";
    string channel = "";
    bool init = false;
    int fails = 0;

    while (!result.CloseStatus.HasValue)
    {
        try
        {
            string message = Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, result.Count));
            var myObject = JsonConvert.DeserializeAnonymousType(message, new { type = "", name = "", channel = "", topic = "" });

            if (myObject != null)
            {
                //Initialize
                if (!init && myObject.type == "init")
                {
                    name = myObject.name;
                    channel = myObject.channel;
                    socketManager.AddSocket(webSocket, channel);
                    BotServer.Print($"{name} has connected to channel: {channel}");
                }
                //Already initialized
                else if (name != "")
                {
                    // Its a ping, ignore it.
                    if (myObject.type == "ping"){ }
                    //Broadcast message to all connections in the channel.
                    else
                    {
                        JObject? jsonObj = JsonConvert.DeserializeObject<JObject>(message);
                        if (jsonObj != null)
                        {
                            jsonObj.Add("name", name);
                            var tasks = new List<Task>();
                            foreach (var socket in socketManager.GetChannelConnections(channel))
                            {
                                tasks.Add(socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonObj))), result.MessageType, result.EndOfMessage, CancellationToken.None));
                            }
                            await Task.WhenAll(tasks);
                        }
                    }
                }
            }

            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        }
        catch (Exception e)
        {
            BotServer.Print("An Exception has happen exception:");
            BotServer.Print(e.ToString());
            BotServer.Print("Result:");
            BotServer.Print(result.ToString() ?? "Null for some reason.");
            fails++;
            if (fails == 3)
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            else if (fails >= 5)
            {
                BotServer.Print($"To many Exception. Dropping {name}");
                break;
            }
        }
    }

    BotServer.Print($"{name} has disconnected.");
    socketManager.RemoveSocket(webSocket, channel);
    await webSocket.CloseAsync(result.CloseStatus != null ? result.CloseStatus.Value : WebSocketCloseStatus.InvalidMessageType, result.CloseStatusDescription, CancellationToken.None);

}


// Catch OS Kill Signal.
PosixSignalRegistration.Create(PosixSignal.SIGTERM, (context) =>
{
    socketManager.StopServer();
});
// Catch OS Kill Signal.
PosixSignalRegistration.Create(PosixSignal.SIGQUIT, (context) =>
{
    socketManager.StopServer();
});
// Catch Ctrl + C
Console.CancelKeyPress += (sender, eventArgs) =>
{
    socketManager.StopServer();
};

BotServer.Print("Bot Server is running!");

app.Start();
await SendPingLoop();
await app.StopAsync();