using Microsoft.IdentityModel.Tokens;

namespace API.SignalR;

public class PresenceTracker
{
    private static readonly Dictionary<string, List<string>> OnlineUsers = 
        new Dictionary<string, List<string>>();

    public Task<bool> UserConnected(string username, string connectionId) {
        bool newOnline = false;

        lock(OnlineUsers)
        {
            if(OnlineUsers.ContainsKey(username))
            {
                OnlineUsers[username].Add(connectionId);
            }
            else
            {
                OnlineUsers.Add(username, new List<string>{connectionId});
                newOnline = true;
            }            
        }

        return Task.FromResult(newOnline);
    }
    
    public Task<bool> UserDisconnected(string username, string connectionId) {
        bool realOffline = false;

        lock(OnlineUsers)
        {
            if(!OnlineUsers.ContainsKey(username)) return Task.FromResult(realOffline);

            OnlineUsers[username].Remove(connectionId);
            
            if(OnlineUsers[username].Count == 0)
            {
                OnlineUsers.Remove(username);
                realOffline = true;
            }          
        }

        return Task.FromResult(realOffline);
    }

    public Task<string[]> GetOnlineUsers() 
    {
        string[] onlineUsers;

        lock(OnlineUsers)
        {
            onlineUsers = OnlineUsers.OrderBy(k => k.Key).Select(k => k.Key).ToArray();
        }

        return Task.FromResult(onlineUsers);
    }

    public static Task<List<string>> GetConnectionsForUser(string username)
    {
        List<string> connectionIds;

        lock(OnlineUsers)
        {
            connectionIds = OnlineUsers.GetValueOrDefault(username);
        }

        return Task.FromResult(connectionIds);
    }
}
