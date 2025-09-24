namespace GoGame.Models
{
    public enum StoneColor
    {
        Empty,
        Black,
        White
    }

    public enum GameResult
    {
        Ongoing,
        BlackWins,
        WhiteWins,
        Draw
    }

    public class Position
    {
        public int Row { get; set; }
        public int Col { get; set; }

        public Position(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override bool Equals(object? obj)
        {
            return obj is Position position && Row == position.Row && Col == position.Col;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Col);
        }
    }

    public class GameState
    {
        public const int BOARD_SIZE = 19;
        
        public StoneColor[,] Board { get; private set; }
        public StoneColor CurrentPlayer { get; private set; }
        public int BlackCaptured { get; private set; }
        public int WhiteCaptured { get; private set; }
        public bool BlackPassed { get; private set; }
        public bool WhitePassed { get; private set; }
        public GameResult Result { get; private set; }
        public List<Position> LastCapturedStones { get; private set; }

        // Ko rule: prevent immediate recapture
        private Position? koPosition;

        public GameState()
        {
            Board = new StoneColor[BOARD_SIZE, BOARD_SIZE];
            CurrentPlayer = StoneColor.Black;
            BlackCaptured = 0;
            WhiteCaptured = 0;
            BlackPassed = false;
            WhitePassed = false;
            Result = GameResult.Ongoing;
            LastCapturedStones = new List<Position>();
        }

        public bool IsValidMove(int row, int col)
        {
            if (Result != GameResult.Ongoing) return false;
            if (row < 0 || row >= BOARD_SIZE || col < 0 || col >= BOARD_SIZE) return false;
            if (Board[row, col] != StoneColor.Empty) return false;
            if (koPosition != null && koPosition.Row == row && koPosition.Col == col) return false;

            // Check if move would be suicide (no liberties and doesn't capture)
            return !IsSuicideMove(row, col);
        }

        public bool MakeMove(int row, int col)
        {
            if (!IsValidMove(row, col)) return false;

            Board[row, col] = CurrentPlayer;
            LastCapturedStones.Clear();
            koPosition = null;

            // Check for captures
            var opponent = CurrentPlayer == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            var capturedPositions = new List<Position>();

            // Check all adjacent opponent groups for capture
            foreach (var adj in GetAdjacentPositions(row, col))
            {
                if (Board[adj.Row, adj.Col] == opponent)
                {
                    var group = GetGroup(adj.Row, adj.Col);
                    if (GetLiberties(group).Count == 0)
                    {
                        capturedPositions.AddRange(group);
                    }
                }
            }

            // Remove captured stones
            foreach (var pos in capturedPositions)
            {
                Board[pos.Row, pos.Col] = StoneColor.Empty;
                LastCapturedStones.Add(pos);
                
                if (CurrentPlayer == StoneColor.Black)
                    BlackCaptured++;
                else
                    WhiteCaptured++;
            }

            // Set Ko position if exactly one stone was captured
            if (capturedPositions.Count == 1)
            {
                koPosition = capturedPositions[0];
            }

            // Switch players
            CurrentPlayer = opponent;
            
            // Reset pass flags when a move is made
            BlackPassed = false;
            WhitePassed = false;

            return true;
        }

        public void Pass()
        {
            if (Result != GameResult.Ongoing) return;

            if (CurrentPlayer == StoneColor.Black)
            {
                BlackPassed = true;
            }
            else
            {
                WhitePassed = true;
            }

            // If both players pass, game ends
            if (BlackPassed && WhitePassed)
            {
                EndGame();
            }
            else
            {
                // Switch players
                CurrentPlayer = CurrentPlayer == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            }
        }

        private bool IsSuicideMove(int row, int col)
        {
            var originalColor = Board[row, col];
            Board[row, col] = CurrentPlayer;

            var group = GetGroup(row, col);
            var hasLiberties = GetLiberties(group).Count > 0;

            // Check if this move captures opponent stones
            var opponent = CurrentPlayer == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            bool capturesOpponent = false;
            
            foreach (var adj in GetAdjacentPositions(row, col))
            {
                if (Board[adj.Row, adj.Col] == opponent)
                {
                    var opponentGroup = GetGroup(adj.Row, adj.Col);
                    if (GetLiberties(opponentGroup).Count == 0)
                    {
                        capturesOpponent = true;
                        break;
                    }
                }
            }

            Board[row, col] = originalColor;
            
            // Move is suicide if it has no liberties and doesn't capture opponent
            return !hasLiberties && !capturesOpponent;
        }

        private List<Position> GetGroup(int row, int col)
        {
            var color = Board[row, col];
            var group = new List<Position>();
            var visited = new HashSet<Position>();
            var stack = new Stack<Position>();
            
            stack.Push(new Position(row, col));
            
            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                if (visited.Contains(pos)) continue;
                
                visited.Add(pos);
                if (Board[pos.Row, pos.Col] == color)
                {
                    group.Add(pos);
                    
                    foreach (var adj in GetAdjacentPositions(pos.Row, pos.Col))
                    {
                        if (!visited.Contains(adj) && Board[adj.Row, adj.Col] == color)
                        {
                            stack.Push(adj);
                        }
                    }
                }
            }
            
            return group;
        }

        private List<Position> GetLiberties(List<Position> group)
        {
            var liberties = new HashSet<Position>();
            
            foreach (var pos in group)
            {
                foreach (var adj in GetAdjacentPositions(pos.Row, pos.Col))
                {
                    if (Board[adj.Row, adj.Col] == StoneColor.Empty)
                    {
                        liberties.Add(adj);
                    }
                }
            }
            
            return liberties.ToList();
        }

        private List<Position> GetAdjacentPositions(int row, int col)
        {
            var adjacent = new List<Position>();
            
            var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
            
            foreach (var (dr, dc) in directions)
            {
                int newRow = row + dr;
                int newCol = col + dc;
                
                if (newRow >= 0 && newRow < BOARD_SIZE && newCol >= 0 && newCol < BOARD_SIZE)
                {
                    adjacent.Add(new Position(newRow, newCol));
                }
            }
            
            return adjacent;
        }

        private void EndGame()
        {
            // Simple territory counting for demo
            var (blackTerritory, whiteTerritory) = CountTerritory();
            
            var blackScore = blackTerritory + BlackCaptured;
            var whiteScore = whiteTerritory + WhiteCaptured + 6.5; // Komi for white
            
            if (blackScore > whiteScore)
                Result = GameResult.BlackWins;
            else if (whiteScore > blackScore)
                Result = GameResult.WhiteWins;
            else
                Result = GameResult.Draw;
        }

        private (int blackTerritory, int whiteTerritory) CountTerritory()
        {
            var visited = new bool[BOARD_SIZE, BOARD_SIZE];
            int blackTerritory = 0, whiteTerritory = 0;

            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    if (!visited[row, col] && Board[row, col] == StoneColor.Empty)
                    {
                        var territory = GetTerritoryGroup(row, col, visited);
                        var owner = GetTerritoryOwner(territory);
                        
                        if (owner == StoneColor.Black)
                            blackTerritory += territory.Count;
                        else if (owner == StoneColor.White)
                            whiteTerritory += territory.Count;
                    }
                }
            }

            return (blackTerritory, whiteTerritory);
        }

        private List<Position> GetTerritoryGroup(int row, int col, bool[,] visited)
        {
            var territory = new List<Position>();
            var stack = new Stack<Position>();
            stack.Push(new Position(row, col));

            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                if (visited[pos.Row, pos.Col]) continue;

                visited[pos.Row, pos.Col] = true;
                if (Board[pos.Row, pos.Col] == StoneColor.Empty)
                {
                    territory.Add(pos);
                    
                    foreach (var adj in GetAdjacentPositions(pos.Row, pos.Col))
                    {
                        if (!visited[adj.Row, adj.Col] && Board[adj.Row, adj.Col] == StoneColor.Empty)
                        {
                            stack.Push(adj);
                        }
                    }
                }
            }

            return territory;
        }

        private StoneColor GetTerritoryOwner(List<Position> territory)
        {
            var surroundingColors = new HashSet<StoneColor>();
            
            foreach (var pos in territory)
            {
                foreach (var adj in GetAdjacentPositions(pos.Row, pos.Col))
                {
                    var color = Board[adj.Row, adj.Col];
                    if (color != StoneColor.Empty)
                    {
                        surroundingColors.Add(color);
                    }
                }
            }

            // Territory belongs to a player only if surrounded by that player's stones
            if (surroundingColors.Count == 1)
            {
                return surroundingColors.First();
            }
            
            return StoneColor.Empty; // Neutral territory
        }

        public void NewGame()
        {
            Board = new StoneColor[BOARD_SIZE, BOARD_SIZE];
            CurrentPlayer = StoneColor.Black;
            BlackCaptured = 0;
            WhiteCaptured = 0;
            BlackPassed = false;
            WhitePassed = false;
            Result = GameResult.Ongoing;
            LastCapturedStones.Clear();
            koPosition = null;
        }
    }
}