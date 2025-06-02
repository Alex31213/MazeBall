using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MazeBall.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> ConnectionUserMapping =
           new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string[]> Rooms = new ConcurrentDictionary<string, string[]>();
        private static ConcurrentDictionary<string, int> MaxPlayers = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, string> UserRooms = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, int> RoomActivePlayers = new ConcurrentDictionary<string, int>();

        private static ConcurrentDictionary<string, int> RoomOrderNumber = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, ConcurrentDictionary<int, string>> RoomPlayersOrder =
           new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>();
        private static ConcurrentDictionary<string, ConcurrentDictionary<int, string>> RoomMessages =
           new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>();
        public GameHub() 
        {
            
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
                    //await StartGame(roomName);
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
    }
}
