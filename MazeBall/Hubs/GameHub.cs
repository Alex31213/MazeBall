using MazeBall.Database.CRUDs;
using MazeBall.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Net;

namespace MazeBall.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly ConcurrentDictionary<string, string> ConnectionUserMapping =
           new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string[]> Rooms = new ConcurrentDictionary<string, string[]>();
        private static ConcurrentDictionary<string, int> MaxPlayers = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, string> UserRooms = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, int> RoomActivePlayers = new ConcurrentDictionary<string, int>();
        ImmutableDictionary<int, string> colors;

        private static ConcurrentDictionary<string, int> RoomOrderNumber = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, ConcurrentDictionary<int, string>> RoomPlayersOrder =
           new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>();
        private static ConcurrentDictionary<string, ConcurrentDictionary<int, string>> RoomMessages =
           new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>();
        public GameHub(IServiceScopeFactory scopeFactory) 
        {
            this._scopeFactory = scopeFactory;
            colors = ImmutableDictionary<int, string>.Empty
                .Add(0, "unchosen")
                .Add(1, "red")
                .Add(2, "purple");
        }
        public override async Task OnConnectedAsync()
        {
            var username = Context.User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
            ConnectionUserMapping[Context.ConnectionId] = username;

            if (!string.IsNullOrEmpty(username) && UserRooms.TryGetValue(username, out var roomName))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
                Console.WriteLine($"{username}' entered game in room '{roomName}'");
                RoomActivePlayers.AddOrUpdate(roomName, 1, (_, count) => count + 1);
                if (RoomActivePlayers.TryGetValue(roomName, out var activePlayers) &&
                    MaxPlayers.TryGetValue(roomName, out var maxPlayers) &&
                    activePlayers == maxPlayers)
                {
                    await SetRoomName(roomName);
                    await StartGame(roomName);
                }
            }
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Remove the mapping when a connection is disconnected
            ConnectionUserMapping.TryRemove(Context.ConnectionId, out var username);

            // Remove the user from the room and update the active players count
            if (!string.IsNullOrEmpty(username) && UserRooms.TryGetValue(username, out var roomName))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

                UserRooms.TryRemove(username, out _);

                if (Rooms.TryGetValue(roomName, out var players))
                {
                    var updatedPlayers = players.Where(player => player != username).ToArray();
                    Rooms[roomName] = updatedPlayers;

                    RoomActivePlayers.AddOrUpdate(roomName, 0, (_, count) => count - 1);

                    if (updatedPlayers.Length == 0)
                    {
                        Rooms.TryRemove(roomName, out _);
                        MaxPlayers.TryRemove(roomName, out _);
                        RoomActivePlayers.TryRemove(roomName, out _);
                        RoomMessages.TryRemove(roomName, out _);
                        RoomPlayersOrder.TryRemove(roomName, out _);
                        RoomOrderNumber.TryRemove(roomName, out _);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task<List<string>> GetRoomNames()
        {
            var roomNames = Rooms.Keys.ToList();
            return await Task.FromResult(roomNames);
        }
        private async Task SetRoomName(string roomName)
        {
            await Clients.Group(roomName).SendAsync("setRoomName", roomName);
        }
        public async Task AddRoomAndPlayers(string roomName, string[] usernames)
        {
            Rooms[roomName] = usernames;
            MaxPlayers[roomName] = usernames.Length;

            Random random = new Random();
            List<int> availableNumbers = Enumerable.Range(1, MaxPlayers[roomName]).ToList();
            RoomMessages.TryAdd(roomName, new ConcurrentDictionary<int, string>());
            RoomOrderNumber[roomName] = 1;
            foreach (var user in usernames)
            {
                int randomIndex = random.Next(0, availableNumbers.Count);
                int randomRoomNumber = availableNumbers[randomIndex];
                availableNumbers.RemoveAt(randomIndex);
                UserRooms[user] = roomName;
                RoomPlayersOrder.TryAdd(roomName, new ConcurrentDictionary<int, string>());
                RoomPlayersOrder[roomName].TryAdd(randomRoomNumber, user);
            }
            await Task.CompletedTask;
        }
        private async Task GetPlayersContainer(string roomName)
        {
            var playersList = new List<object>();
            if (RoomPlayersOrder.TryGetValue(roomName, out var playersOrder))
            {
                foreach (var player in playersOrder)
                {
                    if (colors.TryGetValue(player.Key, out var color))
                    {
                        playersList.Add(new { username = player.Value, color = color });
                    }
                }
            }
            await Clients.Group(roomName).SendAsync("updatePlayersContainer", playersList);
        }

        private async Task StartGame(string roomName)
        {
            Console.WriteLine($"Game started in room {roomName}");
            await AddMatchToDatabase(roomName);
            await GetPlayersContainer(roomName);
        }

        public async Task EndTurn(string roomName)
        {
            RoomOrderNumber[roomName]++;
            if (RoomOrderNumber[roomName] > MaxPlayers[roomName])
            {
                RoomOrderNumber[roomName] = 1;
            }
            await Task.CompletedTask;
        }

        private async Task AddMatchToDatabase(string roomName)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MazeBallContext>();
            var matchToAdd = new Match 
            { 
                CreationDate = DateTime.Now.Date,
                RoomName = roomName,
            };
            MatchCRUD matchCRUD = new MatchCRUD(dbContext);
            matchCRUD.Add(matchToAdd);
            Console.WriteLine($"Added match with roomName {roomName} in database");
            await Task.CompletedTask;
        }

        private async Task UpdateEventText(string roomName, string eventText)
        {
            await Clients.Group(roomName).SendAsync("eventTextUpdate", eventText);
        }

        public async Task SendChatMessage(string roomName, string message)
        {
            string username = ConnectionUserMapping[Context.ConnectionId];
            string toAddMessage = $"{username}: " + message;
            RoomMessages[roomName].TryAdd(RoomMessages[roomName].Count, toAddMessage);
            await Clients.Group(roomName).SendAsync("updateChat", RoomMessages[roomName]);
        }
    }
}
