using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Drawsome.Models;

namespace Drawsome.Hubs
{
    public class LobbyHub : Hub
    {
        public static ConcurrentDictionary<string, Lobby?> Lobbies = new();
        
        public async Task SendDrawing(string lobbyName, string drawingData)
        {
            await Clients.Group(lobbyName).SendAsync("ReceiveDrawing", drawingData);
        }

        public async Task<bool> CreateLobby(string lobbyName, string username)
        {
            if (Lobbies.TryAdd(lobbyName, new Lobby { LobbyName = lobbyName}))
            {
                await JoinLobby(lobbyName, username);
                return true;
            }
            else
            {
                await Clients.Caller.SendAsync("LobbyCreationFailed", lobbyName);
                return false;
            }
        }

        public async Task<bool> JoinLobby(string lobbyName, string username)
        {
            if (Lobbies.TryGetValue(lobbyName, out Lobby? lobby))
            {
                if (lobby != null && lobby.Players.All(p => p != username))
                {
                    lobby.Players.Add(username);
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, lobbyName);
                if (lobby != null) await Clients.Group(lobbyName).SendAsync("UpdatePlayers", lobby.Players);
                return true;
            }
            else
            {
                await Clients.Caller.SendAsync("LobbyNotFound", lobbyName);
                return false;
            }
        }

        public async Task LeaveLobby(string lobbyName, string username)
        {
            if (Lobbies.TryGetValue(lobbyName, out Lobby? lobby))
            {
                if (lobby != null && lobby.Players.Contains(username))
                {
                    lobby.Players.Remove(username);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyName);
                    await Clients.Group(lobbyName).SendAsync("UpdatePlayers", lobby.Players);
                }

                if (lobby != null && lobby.Players.Count == 0)
                {
                    Lobbies.TryRemove(lobbyName, out _);
                }
            }
        }
    }
}