using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace DibraServer
{
    public class SocketManager
    {
        private readonly CancellationTokenSource source = new();
        public readonly CancellationToken ServerLoop;
        public readonly ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, object?>> connections = new();

        public SocketManager()
        {
            ServerLoop = source.Token;
        }

        public void StopServer()
        {
            source.Cancel();
        }
        private bool AddChannel(string channel)
        {
            return connections.TryAdd(channel, new());
        }

        public void AddSocket(WebSocket socket, string channel)
        {
            if (AddChannel(channel))
            {
                BotServer.Print($"Channel added: {channel}");
            }

            _ = connections[channel].TryAdd(socket, new());            
        }

        public void RemoveSocket(WebSocket webSocket, string channel)
        { 
            _ = connections[channel].TryRemove(webSocket, out _);
            if (connections[channel].IsEmpty)
            {
                _ = connections.TryRemove(channel, out _);
                BotServer.Print($"Closing empty channel: {channel}");
            }
        }

        public ICollection<WebSocket> GetChannelConnections(string channel)
        {
            return connections[channel].Keys;    
        }
    }
}
