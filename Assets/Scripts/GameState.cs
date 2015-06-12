using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public interface GameStateListener
{
	void OnStartEnd();
	void OnLogChanged();
	void OnNewTurn();
}

public class GameState
{
	[Serializable]
	public struct Position : IComparable
	{
		public int x;
		public int y;

		public Position(int _x, int _y)
		{
			x = _x;
			y = _y;
		}

		const string lut = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		public override string ToString()
		{
			string name = "";
			int ix = x;
			for (; ; )
			{
				int next = ix / lut.Length;
				int curr = ix % lut.Length;
				name += lut[curr];
				if (next == 0)
					break;

				ix = next;
			}

			name += (y + 1).ToString("D2");
			return name;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Position)) return false;

			Position p = (Position)obj;
			return p.x == x && p.y == y;
		}

		public override int GetHashCode()
		{
			return (((x << 5) + x) ^ y);
		}

		public int CompareTo(object obj)
		{
			if (!(obj is Position)) return -1;

			Position p = (Position)obj;
			int compare_x = x.CompareTo(p.x);
			return compare_x == 0 ? y.CompareTo(p.y) : compare_x;
		}
	}

	public enum CellType
	{
		CleanSector = 0,
		SpecialSector = 1,
		Hollow = 2,
		HumanSpawn = 3,
		AlienSpawn = 4,
		EscapeHatch = 5,
		Count = 6
	}

	public enum SectorState
	{
		None,
		OnNoiseInYourSector,
		OnNoiseInAnySector,
		OnSilenceInAllSectors
	};

	public struct Player
	{
		public enum Type
		{
			Human,
			Alien
		}

		public Type type;
		public Position position;
		public bool alive;
	}

	public const int map_size_y = 14;
	public const int map_size_x = 23;

	private int numPlayers = 0;
	private CellType[,] map = new CellType[map_size_y, map_size_x];

	private Player[] players = new Player[0];
	private int player_turn = 0;
	private int turn_count = 0;

	private Position position_to_move = new Position(-1, -1);
	private SectorState sector_action = SectorState.None;

	private string log = "";
	private Random random = new Random();
	private HashSet<Position> currentWalkablePositions = new HashSet<Position>();

	private HashSet<GameStateListener> listeners = new HashSet<GameStateListener>();

	public void AddListener(GameStateListener listener) { listeners.Add(listener); }
	public CellType GetMapCell(int x, int y) { return map[y, x]; }
	public CellType GetMapCell(Position p) { return GetMapCell(p.x, p.y); }
	public Player CurrentPlayer { get { return players[player_turn]; } }
	public int PlayerTurn { get { return player_turn; } }
	public int TurnCount { get { return turn_count; } }
	public int CurrentRound { get { return (turn_count / numPlayers) + 1; } }
	public SectorState CurrentSectorState { get { return sector_action; } }
	public Position CurrentSectorStatePosition { get { return position_to_move; } }
	public string Log { get { return log; } }
	public IEnumerable<Position> CurrentWalkablePositions
	{
		get
		{
			foreach (Position position in currentWalkablePositions)
				yield return position;
		}
	}

	private void PrintLog(string msg)
	{
		log += msg + "\n";

		foreach (GameStateListener listener in listeners)
			listener.OnLogChanged();
	}

	public void Start(int seed, int _numPlayers, int[,] _map)
	{
		random = new Random(seed);
		numPlayers = _numPlayers;

		if (numPlayers < 2) {
			UnityEngine.Debug.LogError("Minimum 2 players. Defaulting 2.");
			numPlayers = 2;
		}

		map = new CellType[map_size_y, map_size_x];
		Position AlienSpawnPosition = new Position(-1, -1);
		Position HumanSpawnPosition = new Position(-1, -1);

		//Copying the map
		for (int y = 0; y < map_size_y; ++y)
			for (int x = 0; x < map_size_x; ++x)
			{
				CellType type = (CellType)_map[y, x];
				switch (type)
				{
					case CellType.HumanSpawn:
						if (HumanSpawnPosition.x < 0)
							HumanSpawnPosition = new Position(x, y);
						else
							UnityEngine.Debug.LogError("Only can be one Human Spawn in the map.");
						break;
					case CellType.AlienSpawn:
						if (AlienSpawnPosition.x < 0)
							AlienSpawnPosition = new Position(x, y);
						else
							UnityEngine.Debug.LogError("Only can be one Human Spawn in the map.");
						break;
					default:
						break;
				};

				map[y, x] = type;
			}

		if (HumanSpawnPosition.x < 0) {
			UnityEngine.Debug.Log("No human spawn found. The map is invalid.");
			return;
		}

		if (AlienSpawnPosition.x < 0) {
			UnityEngine.Debug.Log("No Alien spawn found. The map is invalid.");
			return;
		}

		//Generating equal num of roles
		int[] roles = new int[numPlayers];
		for (int i = 0; i < numPlayers; i++)
			roles[i] = i % 2;

		// Shuffle
		int n = numPlayers;
		while (n > 1)
		{
			n--;
			int k = random.Next(n + 1);
			int value = roles[k];
			roles[k] = roles[n];
			roles[n] = value;
		}

		// Creating players
		players = new Player[numPlayers];
		for (int i = 0; i < numPlayers; i++)
		{
			players[i] = new Player();
			if (roles[i] == 0)
			{
				players[i].type = Player.Type.Human;
				players[i].position = HumanSpawnPosition;
			}
			else
			{
				players[i].type = Player.Type.Alien;
				players[i].position = AlienSpawnPosition;
			}

			players[i].alive = true;
		}

		player_turn = random.Next(numPlayers);
		turn_count = 0;
		sector_action = SectorState.None;

		foreach (GameStateListener listener in listeners)
			listener.OnStartEnd();

		SetupNewTurn();

		log = "";
		PrintLog("Created game for " + numPlayers + " players.");
	}

	public bool IsWalkeable(Position position)
	{
		return position.x >= 0
			&& position.x < map_size_x
			&& position.y >= 0
			&& position.y < map_size_y
			&& (map[position.y, position.x] == CellType.CleanSector
			|| map[position.y, position.x] == CellType.SpecialSector
			|| map[position.y, position.x] == CellType.EscapeHatch);
	}

	private void AddRelativeHexagon(Position position, int xrel, int yrel, HashSet<Position> positions)
	{
		Position newpos = new Position(position.x + xrel, position.y + yrel);
		if (IsWalkeable(newpos))
		{
			positions.Add(newpos);
		}
	}

	private void AddNeighboursHexagons(Position position, HashSet<Position> positions)
	{
		int yshift = (position.x % 2);
		AddRelativeHexagon(position, -1, -1 + yshift, positions);
		AddRelativeHexagon(position, -1, +0 + yshift, positions);
		AddRelativeHexagon(position, +0, -1, positions);
		AddRelativeHexagon(position, +0, +1, positions);
		AddRelativeHexagon(position, +1, -1 + yshift, positions);
		AddRelativeHexagon(position, +1, +0 + yshift, positions);
	}

	private void SetupNewTurn()
	{
		Player currentPlayer = players[player_turn];

		int steps = 1;
		if (currentPlayer.type == Player.Type.Alien)
			steps = 2;

		currentWalkablePositions.Clear();
		currentWalkablePositions.Add(currentPlayer.position);
		for (int i = 0; i < steps; i++)
		{
			HashSet<GameState.Position> positions = new HashSet<GameState.Position>();
			foreach (GameState.Position position in currentWalkablePositions)
				AddNeighboursHexagons(position, positions);

			foreach (GameState.Position position in positions)
				currentWalkablePositions.Add(position);
		}

		currentWalkablePositions.Remove(currentPlayer.position);
		sector_action = GameState.SectorState.None;

		foreach (GameStateListener listener in listeners)
			listener.OnNewTurn();
	}

	private void InternalMoveOn(GameState.Position position)
	{
		players[player_turn].position = position;
		player_turn = (player_turn + 1) % players.Length;
		turn_count++;
		SetupNewTurn();
	}

	public void TurnSpecialSectorMoveOn(GameState.Position position)
	{
		StringBuilder builder = new StringBuilder();
		builder.Append("[P"); builder.Append((player_turn + 1).ToString());
		builder.Append(":T"); builder.Append(CurrentRound); builder.Append("] ");

		switch (sector_action)
		{
			case GameState.SectorState.None:
				break;
			case GameState.SectorState.OnNoiseInYourSector:
				builder.Append("Noise in sector " + position_to_move.ToString());
				break;
			case GameState.SectorState.OnNoiseInAnySector:
				builder.Append("Noise in sector " + position.ToString());
				break;
			case GameState.SectorState.OnSilenceInAllSectors:
				builder.Append("Silence in all sectors");
				break;
		}

		PrintLog(builder.ToString());
		InternalMoveOn(position_to_move);
	}

	public void TurnMoveOn(GameState.Position position)
	{
		if (sector_action != GameState.SectorState.None)
			return;

		if (currentWalkablePositions.Contains(position))
		{
			GameState.CellType type = map[position.y, position.x];
			switch (type)
			{
				case GameState.CellType.CleanSector:
					break;
				case GameState.CellType.SpecialSector:
					position_to_move = position;
					int rand = random.Next(5);
					if (rand == 0 || rand == 1)
						sector_action = GameState.SectorState.OnNoiseInYourSector;
					else if (rand == 2 || rand == 3)
						sector_action = GameState.SectorState.OnNoiseInAnySector;
					else
						sector_action = GameState.SectorState.OnSilenceInAllSectors;
					break;
			}

			if (type == GameState.CellType.CleanSector)
			{
				StringBuilder builder = new StringBuilder();
				builder.Append("[P"); builder.Append((player_turn + 1).ToString());
				builder.Append(":T"); builder.Append(CurrentRound); builder.Append("] ");
				builder.Append("Moved to safe sector.");

				PrintLog(builder.ToString());
				InternalMoveOn(position);
			}
		}
	}
}
