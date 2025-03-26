using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Drawsome.Models;

namespace Drawsome.Hubs
{
    public class LobbyHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Lobby> Lobbies = new ConcurrentDictionary<string, Lobby>();

        public async Task CreateLobby(string lobbyName, string username)
        {
            if (Lobbies.TryAdd(lobbyName, new Lobby { LobbyName = lobbyName, Creator = username }))
            {
                await JoinLobby(lobbyName, username);
            }
        }

        public async Task JoinLobby(string lobbyName, string username)
        {
            if (Lobbies.TryGetValue(lobbyName, out Lobby lobby))
            {
                lobby.Players.Add(username);
                await Groups.AddToGroupAsync(Context.ConnectionId, lobbyName);
                await Clients.Group(lobbyName).SendAsync("UpdatePlayers", lobby.Players);
            }
            else
            {
                await Clients.Caller.SendAsync("LobbyNotFound", lobbyName);
            }
        }

        public async Task LeaveLobby(string lobbyName, string username)
        {
            if (Lobbies.TryGetValue(lobbyName, out Lobby lobby))
            {
                lobby.Players.Remove(username);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyName);
                await Clients.Group(lobbyName).SendAsync("UpdatePlayers", lobby.Players);

                if (lobby.Players.Count == 0 && lobby.Creator == username)
                {
                    Lobbies.TryRemove(lobbyName, out _);
                }
            }
        }
    }
}