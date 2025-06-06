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
        public class Position
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly ConcurrentDictionary<string, string> ConnectionUserMapping =
           new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string[]> Rooms = new ConcurrentDictionary<string, string[]>();
        private static ConcurrentDictionary<string, int> MaxPlayers = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, string> UserRooms = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, int> RoomActivePlayers = new ConcurrentDictionary<string, int>();
        ImmutableDictionary<int, string> colors;
        private const int mazeMatrixHeight = 16;
        private const int mazeMatrixWidth = 16;

        private static ConcurrentDictionary<string, int> RoomOrderNumber = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, ConcurrentDictionary<int, string>> RoomPlayersOrder =
           new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>();
        private static ConcurrentDictionary<string, ConcurrentDictionary<int, string>> RoomMessages =
           new ConcurrentDictionary<string, ConcurrentDictionary<int, string>>();
        private static ConcurrentDictionary<string, ConcurrentDictionary<int, ConcurrentDictionary<int, int>>>
            RoomMaze = new ConcurrentDictionary<string, ConcurrentDictionary<int,
                ConcurrentDictionary<int, int>>>();
        private static ConcurrentDictionary<string, ConcurrentDictionary<int, Position>> RoomMazeBallPositions =
            new ConcurrentDictionary<string, ConcurrentDictionary<int, Position>>();
        private static ConcurrentDictionary<string, int> RoomReceivedFinalPositions =
            new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, int> RoomWinnerId = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, int> RoomReceivedWinnerConfirmation =
            new ConcurrentDictionary<string, int>();

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
                        RoomMaze.TryRemove(roomName, out _);
                        RoomMazeBallPositions.TryRemove(roomName, out _);
                        RoomReceivedFinalPositions.TryRemove(roomName, out _);
                        RoomReceivedWinnerConfirmation.TryRemove(roomName, out _);
                        RoomWinnerId.TryRemove(roomName, out _);
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
            RoomMazeBallPositions.TryAdd(roomName, new ConcurrentDictionary<int, Position>());
            RoomReceivedFinalPositions.TryAdd(roomName, 0);
            RoomWinnerId.TryAdd(roomName, -1);
            RoomReceivedWinnerConfirmation.TryAdd(roomName, 0);
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
            await GenerateRandomMazeMatrix(roomName, mazeMatrixHeight, mazeMatrixWidth);
            await StartTurn(roomName);
        }

        private async Task StartTurn(string roomName)
        {
            string currentPlayerUsername = RoomPlayersOrder[roomName][RoomOrderNumber[roomName]];
            string eventText = $"{currentPlayerUsername} turn to take the shot!";
            await UpdateEventText(roomName, eventText);
            await SendTurnNotification(roomName, currentPlayerUsername);
        }

        private async Task SendTurnNotification(string roomName, string playerUsername)
        {
            string connectionId = ConnectionUserMapping.FirstOrDefault(pair => pair.Value == playerUsername).Key;
            await Clients.Client(connectionId).SendAsync("startTurnButton", RoomOrderNumber[roomName]);
        }

        public async Task SendTurnActiveBallPositions(string roomName, double vx, double vy)
        {
            await Clients.Groups(roomName).SendAsync("moveTurnBalls", RoomOrderNumber[roomName], vx, vy);
        }
        public async Task EndTurn(string roomName)
        {
            RoomOrderNumber[roomName]++;
            if (RoomOrderNumber[roomName] > MaxPlayers[roomName])
            {
                RoomOrderNumber[roomName] = 1;
            }
            await StartTurn(roomName);
        }

        public ConcurrentDictionary<int, Position> ConvertPositionsListToConcurrent(List<List<double>> positions)
        {
            var concurrentDict = new ConcurrentDictionary<int, Position>();

            for (int i = 0; i < positions.Count; i++)
            {
                var posList = positions[i];
                if (posList.Count >= 2)
                {
                    var pos = new Position
                    {
                        X = posList[0],
                        Y = posList[1]
                    };

                    concurrentDict.TryAdd(i, pos);
                }
            }

            return concurrentDict;
        }

        public async Task CheckFinalPositions(string roomName, List<List<double>> positions)
        {
            ConcurrentDictionary<int, Position> concurrentPositions = ConvertPositionsListToConcurrent(positions);
            RoomReceivedFinalPositions[roomName]++;
            if (RoomReceivedFinalPositions[roomName] == 1) // First received in a player turn
            {
                RoomMazeBallPositions[roomName] = concurrentPositions;
                return;
            }
            if (RoomReceivedFinalPositions[roomName] > 1)
            {
                if (RoomMazeBallPositions[roomName].Count != concurrentPositions.Count)
                {
                    Console.WriteLine("Error in room " + roomName + " . Received " +
                        "ball positions between players don't match");
                }

                foreach (var kvp in RoomMazeBallPositions[roomName])
                {
                    if (!concurrentPositions.TryGetValue(kvp.Key, out Position pos2))
                    {
                        Console.WriteLine("Error in room " + roomName + " . Received " +
                            "ball positions between players don't match");
                    }

                    var pos1 = kvp.Value;

                    if (pos1.X != pos2.X || pos1.Y != pos2.Y)
                    {
                        Console.WriteLine("Error in room " + roomName + " . Received " +
                            "ball positions between players don't match");
                    }
                }
            }
            if (RoomReceivedFinalPositions[roomName] == MaxPlayers[roomName])
            {
                RoomReceivedFinalPositions[roomName] = 0;
                await EndTurn(roomName);
            }
        }

        public async Task CheckVictory(string roomName, int id)
        {
            RoomReceivedWinnerConfirmation[roomName]++;
            if (RoomReceivedFinalPositions[roomName] == 1)
            {
                RoomWinnerId[roomName] = id;
                return;
            }
            if (RoomReceivedWinnerConfirmation[roomName] > 1)
            {
                if (RoomWinnerId[roomName] != id)
                {
                    Console.WriteLine("Error in room " + roomName + " . Received " +
                        "winner's id between players don't match");
                }
            }
            if (RoomReceivedWinnerConfirmation[roomName] == MaxPlayers[roomName])
            {
                Console.WriteLine($"{RoomPlayersOrder[roomName][id]} won the game in room {roomName}.");
                string winnerText = $"{RoomPlayersOrder[roomName][id]} won the game. " +
                    $"Congratulations!";
                await Clients.Groups(roomName).SendAsync("endGame", winnerText);
            }
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

        private async Task GenerateRandomMazeMatrix(string roomName, int height, int width)
        {
            int mazeWidth = (width % 2 == 0) ? width + 1 : width;
            int mazeHeight = (height % 2 == 0) ? height + 1 : height;
            int startX = mazeHeight / 2 + 1;
            int startY = 0;
            int endX = mazeHeight / 2 + 1;
            int endY = mazeWidth - 1;
            int addedColumns = 3;
            int mazeExpandFactor = 3;

            //Initialization
            List<List<int>> generatedMaze = new List<List<int>>(mazeHeight);
            for (int y = 0; y < mazeHeight; y++)
            {
                var row = new List<int>(new int[mazeWidth]);
                for (int x = 0; x < mazeWidth; x++)
                    row[x] = 1;
                generatedMaze.Add(row);
            }
            generatedMaze[startX][startY] = 0;
            generatedMaze[endX][endY] = 0;

            await GenerateMaze(generatedMaze, startX, startY + 1, mazeHeight, mazeWidth);
            Console.WriteLine($"Generated maze for room {roomName}");

            //Add players start
            for (int y = 0; y < mazeHeight; y++)
            {
                for (int i = 0; i < addedColumns; i++)
                {
                    generatedMaze[y].Insert(0, 1);
                }
            }
            mazeWidth = mazeWidth + addedColumns;
            int newStartY = startY + addedColumns;
            int newEndY = endY + addedColumns;
            generatedMaze[endX][newEndY] = 2;
            generatedMaze[startX][newStartY - 1] = 0;
            generatedMaze[startX - 1][newStartY - 1] = 0;
            generatedMaze[startX - 2][newStartY - 1] = 0;
            generatedMaze[startX - 3][newStartY - 1] = 0;
            generatedMaze[startX - 3][newStartY - 2] = 3;
            generatedMaze[startX + 1][newStartY - 1] = 0;
            generatedMaze[startX + 2][newStartY - 1] = 0;
            generatedMaze[startX + 3][newStartY - 1] = 0;
            generatedMaze[startX + 3][newStartY - 2] = 4;

            //Test
            //for (int y = 0; y < generatedMaze.Count; y++)
            //{
            //    for (int x = 0; x < generatedMaze[0].Count; x++)
            //    {
            //        Console.Write(generatedMaze[y][x] == 1 ? "#" : "");
            //        Console.Write(generatedMaze[y][x] == 0 ? " " : "");
            //        Console.Write(generatedMaze[y][x] == 2 ? "2" : "");
            //        Console.Write(generatedMaze[y][x] == 3 ? "3" : "");
            //        Console.Write(generatedMaze[y][x] == 4 ? "4" : "");
            //    }
            //    Console.WriteLine();
            //}

            generatedMaze = await ExpandMaze(generatedMaze, mazeExpandFactor, mazeHeight, mazeWidth);
            Console.WriteLine($"Expanded maze with factor = {mazeExpandFactor} for room {roomName}");

            //Test
            //Console.WriteLine();
            //for (int y = 0; y < generatedMaze.Count; y++)
            //{
            //    for (int x = 0; x < generatedMaze[0].Count; x++)
            //    {
            //        Console.Write(generatedMaze[y][x] == 1 ? "#" : "");
            //        Console.Write(generatedMaze[y][x] == 0 ? " " : "");
            //        Console.Write(generatedMaze[y][x] == 2 ? "2" : "");
            //        Console.Write(generatedMaze[y][x] == 3 ? "3" : "");
            //        Console.Write(generatedMaze[y][x] == 4 ? "4" : "");
            //    }
            //    Console.WriteLine();
            //}

            await ConvertGeneratedMazeList(roomName, generatedMaze);
            Console.WriteLine($"Converted maze for room {roomName}");

            await Clients.Group(roomName).SendAsync("generateMaze", RoomMaze[roomName]);
        }

        private async Task GenerateMaze(List<List<int>> maze, int x, int y, int mazeHeight, int mazeWidth)
        {
            maze[y][x] = 0;
            List<int> dirs = new List<int> { 0, 1, 2, 3 };
            List<int> dx = new List<int> { 0, 0, -1, 1 };
            List<int> dy = new List<int> { -1, 1, 0, 0 };
            await Shuffle(dirs);

            foreach (var dir in dirs)
            {
                int nx = x + dx[dir] * 2;
                int ny = y + dy[dir] * 2;
                bool result = await IsInBounds(nx, ny, mazeHeight, mazeWidth);

                if (result && maze[ny][nx] == 1)
                {
                    maze[y + dy[dir]][x + dx[dir]] = 0;
                    await GenerateMaze(maze, nx, ny, mazeHeight, mazeWidth);
                }
            }

            await Task.CompletedTask;
        }

        private async Task Shuffle(List<int> list)
        {
            Random rand = new Random();

            for (int i = 0; i < list.Count; i++)
            {
                int j = rand.Next(i, list.Count);
                int temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }

            await Task.CompletedTask;
        }

        private async Task<bool> IsInBounds(int x, int y, int height, int width)
        {
            bool result = x > 0 && x < width - 1 && y > 0 && y < height - 1;
            return await Task.FromResult(result);
        }

        public async Task<List<List<int>>> ExpandMaze(List<List<int>> maze, int factor, int mazeHeight, int mazeWidth)
        {
            int newHeight = mazeHeight * factor;
            int newWidth = mazeWidth * factor;

            var expandedMaze = new List<List<int>>(newHeight);

            for (int y = 0; y < newHeight; y++)
            {
                var newRow = new List<int>(newWidth);
                for (int i = 0; i < newWidth; i++)
                    newRow.Add(0);
                expandedMaze.Add(newRow);
            }

            for (int y = 0; y < mazeHeight; y++)
            {
                for (int x = 0; x < mazeWidth; x++)
                {
                    for (int dy = 0; dy < factor; dy++)
                    {
                        for (int dx = 0; dx < factor; dx++)
                        {
                            if (maze[y][x] == 0 || maze[y][x] == 1 || maze[y][x] == 2)
                            {
                                expandedMaze[y * factor + dy][x * factor + dx] = maze[y][x];
                            }
                            else if (maze[y][x] == 3 || maze[y][x] == 4)
                            {
                                expandedMaze[y * factor + dy][x * factor + dx] = 0;
                                if (dy == 1 && dx == 1)
                                {
                                    expandedMaze[y * factor + dy][x * factor + dx] = maze[y][x];
                                }
                            }
                        }
                    }
                }
            }

            return await Task.FromResult(expandedMaze);
        }

        private async Task ConvertGeneratedMazeList(string roomName, List<List<int>> generatedMaze)
        {
            var concurrentMaze = new ConcurrentDictionary<int, ConcurrentDictionary<int, int>>();

            for (int y = 0; y < generatedMaze.Count; y++)
            {
                var row = generatedMaze[y];
                var innerDict = new ConcurrentDictionary<int, int>();

                for (int x = 0; x < row.Count; x++)
                {
                    innerDict[x] = row[x];
                }

                concurrentMaze[y] = innerDict;
            }

            RoomMaze.TryAdd(roomName, concurrentMaze);

            await Task.CompletedTask;
        }
    }
}
