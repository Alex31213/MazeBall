using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MazeBall.Hubs
{
    [Authorize]
    public class LobbyHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> ConnectionUserMapping =
       new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string[]> Rooms = new ConcurrentDictionary<string, string[]>();
        private static ConcurrentDictionary<string, int> MaxPlayers = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, string> UserRooms = new ConcurrentDictionary<string, string>();
        private readonly GameHub _gameHub;

        public LobbyHub(GameHub gameHub)
        {
            _gameHub = gameHub;
        }

        public override async Task OnConnectedAsync()
        {
            // Extract the username from the JWT bearer token
            var username = Context.User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;

            // Map the connection ID to the username
            ConnectionUserMapping[Context.ConnectionId] = username;

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Remove the mapping when a connection is disconnected
            ConnectionUserMapping.TryRemove(Context.ConnectionId, out var username);

            if (!string.IsNullOrEmpty(username) && UserRooms.TryGetValue(username, out var roomName))
            {
                UserRooms.TryRemove(username, out _);

                if (Rooms.TryGetValue(roomName, out var players))
                {
                    var updatedPlayers = players.Where(player => player != username).ToArray();
                    Rooms[roomName] = updatedPlayers;

                    if (updatedPlayers.Length == 0)
                    {
                        Rooms.TryRemove(roomName, out _);
                        MaxPlayers.TryRemove(roomName, out _);
                    }
                    await Clients.All.SendAsync("updateRooms", Rooms, MaxPlayers);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
        public async Task<string> CreateRoom(string roomName, int maxPlayers)
        {
            if (Rooms.ContainsKey(roomName))
            {
                Console.WriteLine($"Room '{roomName}' already exists.");
                return "RoomExists";
            }

            var startedRoomNames = await _gameHub.GetRoomNames();

            if (startedRoomNames.Contains(roomName))
            {
                Console.WriteLine($"A room with name '{roomName}' is already in-game.");
                return "RoomInGame";

            }

            var username = ConnectionUserMapping[Context.ConnectionId];

            if (UserRooms.ContainsKey(username))
            {
                Console.WriteLine($"User '{username}' is already in another room.");
                return "UserInAnotherRoom";
            }

            Rooms[roomName] = new[] { username };
            MaxPlayers[roomName] = maxPlayers;
            UserRooms[username] = roomName;

            Console.WriteLine($"Room '{roomName}' created by '{username}' with max players: {maxPlayers}.");

            await Clients.All.SendAsync("updateRooms", Rooms, MaxPlayers);
            return "RoomCreated";
        }

        public async Task<string> JoinRoom(string roomName)
        {
            if (!Rooms.ContainsKey(roomName))
            {
                Console.WriteLine($"Room '{roomName}' does not exist.");
                return "RoomNotFound";
            }

            var username = ConnectionUserMapping[Context.ConnectionId];

            string[] players = Rooms[roomName];
            int maxPlayers = MaxPlayers[roomName];

            if (Array.IndexOf(players, username) >= 0)
            {
                Console.WriteLine($"User '{username}' is already in room '{roomName}'.");
                return "UserAlreadyInRoom";
            }

            if (UserRooms.ContainsKey(username))
            {
                Console.WriteLine($"User '{username}' is already in another room.");
                return "UserInAnotherRoom";
            }

            if (players.Length >= maxPlayers)
            {
                Console.WriteLine($"Room '{roomName}' is already full. Cannot join.");
                return "RoomIsFull";
            }

            string[] updatedPlayers = new string[players.Length + 1];
            Array.Copy(players, updatedPlayers, players.Length);
            updatedPlayers[players.Length] = username;

            Rooms[roomName] = updatedPlayers;
            UserRooms[username] = roomName;

            Console.WriteLine($"User '{username}' joined room '{roomName}'.");

            if (updatedPlayers.Length == maxPlayers)
            {
                string[] connections = ConnectionUserMapping
                    .Where(pair => updatedPlayers.Contains(pair.Value))
                    .Select(pair => pair.Key)
                    .ToArray();

                foreach (string connection in connections)
                {
                    await Clients.Client(connection).SendAsync("startGame");
                    ConnectionUserMapping.TryRemove(connection, out var usernameConnection);
                    UserRooms.TryRemove(usernameConnection, out _);
                }

                await _gameHub.AddRoomAndPlayers(roomName, updatedPlayers);
                Rooms.TryRemove(roomName, out _);
                MaxPlayers.TryRemove(roomName, out _);

                Console.WriteLine($"Starting game in room: '{roomName}'.");
                await Clients.All.SendAsync("updateRooms", Rooms, MaxPlayers);
                return "GameStarted";
            }
            await Clients.All.SendAsync("updateRooms", Rooms, MaxPlayers);
            return "RoomJoined";
        }


        public async Task LeaveRoom()
        {
            var username = ConnectionUserMapping[Context.ConnectionId];

            if (!UserRooms.ContainsKey(username))
            {
                Console.WriteLine($"User '{username}' is not in any room.");
                return;
            }

            string roomName = UserRooms[username];
            string[] currentPlayers = Rooms[roomName];
            int index = Array.IndexOf(currentPlayers, username);

            string[] updatedPlayers = new string[currentPlayers.Length - 1];
            Array.Copy(currentPlayers, 0, updatedPlayers, 0, index);
            Array.Copy(currentPlayers, index + 1, updatedPlayers, index, updatedPlayers.Length - index);

            if (updatedPlayers.Length == 0)
            {
                Rooms.TryRemove(roomName, out _);
                MaxPlayers.TryRemove(roomName, out _);
                UserRooms.TryRemove(username, out _);
            }
            else
            {
                Rooms[roomName] = updatedPlayers;
                UserRooms.TryRemove(username, out _);
            }

            Console.WriteLine($"User '{username}' left room '{roomName}'.");

            await Clients.All.SendAsync("updateRooms", Rooms, MaxPlayers);
        }

        public async Task UpdateRooms()
        {
            await Clients.Caller.SendAsync("updateRooms", Rooms, MaxPlayers);
        }
    }
}
