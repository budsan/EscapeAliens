using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class HexMatrix : MonoBehaviour {

	// Galilei
	// http://www.escapefromthealiensinouterspace.com/imgs/maps/galilei.jpg
	private int[,] typemat = new int[,]
	{
		{2,1,0,2,2,0,1,0,0,0,1,1,1,1,2,0,0,0,2,2,0,0,2},
		{1,5,1,1,0,1,1,0,1,1,0,0,0,1,1,1,1,1,1,1,1,5,1},
		{1,1,1,1,1,1,1,0,1,1,1,1,1,0,2,0,1,1,2,2,1,1,0},
		{0,1,1,2,1,1,1,1,1,1,1,0,1,1,2,0,0,0,1,2,1,1,0},
		{0,0,1,1,1,1,1,2,1,1,0,1,0,1,0,1,1,1,1,1,0,1,0},
		{0,1,1,2,1,1,1,1,2,1,1,4,1,1,1,1,0,0,1,1,1,1,0},
		{2,2,1,2,2,1,0,0,1,2,2,2,2,2,1,1,1,0,1,0,1,2,2},
		{2,1,1,1,1,1,1,1,1,1,1,3,1,1,1,1,1,0,1,0,1,0,2},
		{0,1,1,1,1,1,1,1,0,1,0,0,0,1,0,1,1,1,1,2,1,1,1},
		{0,0,1,0,1,0,1,2,1,1,1,1,1,1,1,1,1,2,2,2,1,1,0},
		{0,1,1,1,1,1,1,2,1,1,0,0,0,1,1,1,0,2,2,1,1,1,0},
		{0,1,1,1,0,2,0,1,2,2,1,1,1,1,1,0,1,0,1,1,0,1,0},
		{0,5,1,1,1,2,1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,5,1},
		{1,1,0,0,2,2,0,0,0,0,1,0,1,0,0,1,0,2,2,0,1,1,1}
	};

	private const int m_size_y = 14;
	private const int m_size_x = 23;

	public bool buildInEditor = false;

	[SerializeField]
	public int numPlayers = 4;

	[SerializeField]
	public GameObject HexagonPrefab;

	[SerializeField]
	public Vector2 stride = new Vector2(1.50f, 1.70f);

	[SerializeField]
	public Material[] material;

	[SerializeField]
	public UnityEngine.UI.Text LogText;

	[SerializeField]
	private Hexagon[,] cells = null;

	[SerializeField]
	private Hexagon.Position AlienSpawnPosition = new Hexagon.Position(-1, -1);

	[SerializeField]
	private Hexagon.Position HumanSpawnPosition = new Hexagon.Position(-1, -1);

	public enum SpecialSectorType
	{
		None,
		NoiseInYourSector,
		NoiseInAnySector,
		SilenceInAllSectors
	};

	private struct GameState
	{
		public struct Player
		{
			public enum Type
			{
				Human,
				Alien
			}

			public Type type;
			public Hexagon.Position position;
			public bool alive;
		}

		public Player[] players;
		public int player_turn;
		public int turn_count;

		public Hexagon.Position position_to_move;
		public SpecialSectorType sector_action;
	}

	private GameState m_state;

	private System.Random m_random;

	private Hexagon.Position m_click = new Hexagon.Position(-1, -1);
	private HashSet<Hexagon.Position> m_currentWalkablePositions = new HashSet<Hexagon.Position>();
	private Hexagon.Position m_highlighted = new Hexagon.Position(-1, -1);

	public void PrintLog(string msg)
	{
		if (LogText != null)
		{
			LogText.text += msg + "\n";
		}
		else
		{
			Debug.Log(msg);
		}
	}

	public bool ValidSectorSelected()
	{
		return IsWalkeable(m_click);
	}

	public int TurnCount()
	{
		try
		{
			return (m_state.turn_count / m_state.players.Length) + 1;
		}
		catch
		{
			return 1;
		}
	}

	public int PlayerTurn()
	{
		return m_state.player_turn;
	}

	public bool IsHumanPlaying()
	{
		try
		{
			return m_state.players[m_state.player_turn].type == GameState.Player.Type.Human;
		}
		catch
		{
			return false;
		}
	}

	public SpecialSectorType CurrentSectorAction()
	{
		return m_state.sector_action;
	}

	public Hexagon.Position GetCurrentPlayerPosition()
	{
		return m_state.players[m_state.player_turn].position;
	}

	bool CheckMap()
	{
		try
		{
			if (cells == null)
				return false;

			for (int y = 0; y < m_size_y; ++y)
			{
				for (int x = 0; x < m_size_x; ++x)
				{
					if (cells[y, x] == null)
						return false;
				}
			}
		}
		catch(Exception ex)
		{
			Debug.Log("CheckMap: " + ex.Message);
			return false;
		}

		return true;
	}

	//This only should run in editor time
	void Init()
	{
		cells = new Hexagon[m_size_y, m_size_x];

		if (material.Length < (int)Hexagon.Type.Count)
		{
			Debug.LogError("You must set at least " + (int)Hexagon.Type.Count + " textures.");
			return;
		}

		for (int i = 0; i < (int)Hexagon.Type.Count; i++)
		{
			if (material[i] == null)
			{
				Debug.LogError("You must set all textures. Your texture " + i + " isn't set.");
				return;
			}
		}

		if (HexagonPrefab == null)
		{
			Debug.LogError("No prefab assigned for creating playground.");
			return;
		}
		
		AlienSpawnPosition = new Hexagon.Position(-1, -1);
		HumanSpawnPosition = new Hexagon.Position(-1, -1);

		Vector2 halfsize = new Vector2(stride.x * m_size_x, stride.y * m_size_y) * -0.5f;
		int hatchNum = 0;
		for (int y = 0; y < m_size_y; ++y)
		{
			for (int x = 0; x < m_size_x; ++x)
			{
				int type = typemat[y, x];
				if (type < (int)Hexagon.Type.Count)
				{
					Hexagon.Position hexpos = new Hexagon.Position(x, y);
					string hexName = "";

					Hexagon.Type hexType = (Hexagon.Type)type;
					switch (hexType)
					{
						case Hexagon.Type.CleanSector:
							hexName = hexpos.ToString();
							break;
						case Hexagon.Type.SpecialSector:
							hexName = hexpos.ToString();
							break;
						case Hexagon.Type.Hollow:
							
							break;
						case Hexagon.Type.HumanSpawn:
							if (HumanSpawnPosition.x < 0)
								HumanSpawnPosition = hexpos;
							else
								Debug.LogError("Only can be one Human Spawn in the map.");
							break;
						case Hexagon.Type.AlienSpawn:
							if (AlienSpawnPosition.x < 0)
								AlienSpawnPosition = hexpos;
							else
								Debug.LogError("Only can be one Human Spawn in the map.");
							break;
						case Hexagon.Type.EscapeHatch:
							hexName = (++hatchNum).ToString();
							break;
						default:
							break;
					}

					string gameObjectName = "Hex " + hexpos.ToString();	
					Transform meshTransform = transform.Find(gameObjectName);
					GameObject mesh;
					if (meshTransform != null)
						mesh = meshTransform.gameObject;
					else
						mesh = (GameObject) Instantiate(HexagonPrefab);
					
					mesh.name = "Hex " + hexpos.ToString();
					mesh.transform.SetParent(transform, false);
					mesh.transform.SetAsLastSibling();
					mesh.transform.localPosition = new Vector3(x * stride.x + halfsize.x, 0, -y * stride.y - halfsize.y + (x % 2) * 0.5f * -stride.y);

					Hexagon cell = mesh.GetComponent<Hexagon>();
					cells[y, x] = mesh.GetComponent<Hexagon>();

					cell.Init(this, hexpos, (Hexagon.Type)type, material[type], hexName);
					cell.enabled = true;
					cell.interactable = false;
				}
			}
		}

		if (HumanSpawnPosition.x < 0)
		{
			Debug.Log("No human spawn found. The map is invalid.");
			return;
		}

		if (AlienSpawnPosition.x < 0)
		{
			Debug.Log("No Alien spawn found. The map is invalid.");
			return;
		}
	}

	void OnValidate()
	{
		if (buildInEditor && !CheckMap())
			Init();

		if (!buildInEditor)
		{
#if UNITY_EDITOR
			foreach (Transform child in transform)
			{
				GameObject toDestroy = child.gameObject;
				UnityEditor.EditorApplication.delayCall += () => {
					DestroyImmediate(toDestroy);
				};
			}
#endif
			cells = null;
		}
	}

	// Use this for initialization
	void Start ()
	{
		if (buildInEditor && !CheckMap())
		{
			Debug.LogError("Checkmap not passed.");
			return;
		}

		if (!buildInEditor)
			Init();
			
		if (LogText != null)
			LogText.text = "";

		m_random = new System.Random((int)DateTime.Now.Ticks & 0x0000FFFF);
		m_state = new GameState();

		if (numPlayers < 2)
			numPlayers = 2;

		//Generating equal num of roles
		int[] roles = new int[numPlayers];
		for (int i = 0; i < numPlayers; i++)
			roles[i] = i % 2;

		// Shuffle
		int n = numPlayers;
		while (n > 1) {
			n--;
			int k = m_random.Next(n + 1);
			int value = roles[k];
			roles[k] = roles[n];
			roles[n] = value;
		}

		// Creating players
		m_state.players = new GameState.Player[numPlayers];
		for (int i = 0; i < numPlayers; i++)
		{
			m_state.players[i] = new GameState.Player();
			if (roles[i] == 0)
			{
				m_state.players[i].type = GameState.Player.Type.Human;
				m_state.players[i].position = HumanSpawnPosition;
			}
			else
			{
				m_state.players[i].type = GameState.Player.Type.Alien;
				m_state.players[i].position = AlienSpawnPosition;
			}

			m_state.players[i].alive = true;
		}
		
		m_state.player_turn = m_random.Next(numPlayers);
		m_state.turn_count = 0;
		m_state.sector_action = SpecialSectorType.None;

		PrintLog("Created game for " + numPlayers + " players.");

		SetupNewTurn();
	}

	private bool IsWalkeable(Hexagon.Position position)
	{
		return position.x >= 0
			&& position.x < m_size_x
			&& position.y >= 0
			&& position.y < m_size_y
			&&(typemat[position.y, position.x] == (int)Hexagon.Type.CleanSector
			|| typemat[position.y, position.x] == (int)Hexagon.Type.SpecialSector
			|| typemat[position.y, position.x] == (int)Hexagon.Type.EscapeHatch);
	}

	private void AddRelativeHexagon(Hexagon.Position position, int xrel, int yrel, HashSet<Hexagon.Position> positions)
	{
		Hexagon.Position newpos = new Hexagon.Position(position.x + xrel, position.y + yrel);
		if (IsWalkeable(newpos)) {
			positions.Add(newpos);
		}
	}

	private void AddNeighboursHexagons(Hexagon.Position position, HashSet<Hexagon.Position> positions)
	{
		int yshift = (position.x % 2);
		AddRelativeHexagon(position, -1, -1 + yshift, positions);
		AddRelativeHexagon(position, -1, +0 + yshift, positions);
		AddRelativeHexagon(position, +0, -1         , positions);
		AddRelativeHexagon(position, +0, +1         , positions);
		AddRelativeHexagon(position, +1, -1 + yshift, positions);
		AddRelativeHexagon(position, +1, +0 + yshift, positions);
	}

	private void SetupNewTurn()
	{
		Hexagon.Position pos = new Hexagon.Position(0, 0);
		for (pos.y = 0; pos.y < m_size_y; ++pos.y)
			for (pos.x = 0; pos.x < m_size_x; ++pos.x) {
					cells[pos.y, pos.x].highlight = Hexagon.Highlight.None;
					cells[pos.y, pos.x].interactable = false;
				}

		GameState.Player currentPlayer = m_state.players[m_state.player_turn];

		int steps = 1;
		if (currentPlayer.type == GameState.Player.Type.Alien)
			steps = 2;

		m_currentWalkablePositions.Clear();
		m_currentWalkablePositions.Add(currentPlayer.position);
		for (int i = 0; i < steps; i++)
		{
			HashSet<Hexagon.Position> positions = new HashSet<Hexagon.Position>();
			foreach (Hexagon.Position position in m_currentWalkablePositions)
				AddNeighboursHexagons(position, positions);

			foreach (Hexagon.Position position in positions)
				m_currentWalkablePositions.Add(position);
		}

		m_currentWalkablePositions.Remove(currentPlayer.position);
		m_highlighted = currentPlayer.position;
		cells[m_highlighted.y, m_highlighted.x].highlight = Hexagon.Highlight.Current;

		foreach (Hexagon.Position position in m_currentWalkablePositions)
			cells[position.y, position.x].interactable = true;

		m_click = new Hexagon.Position(-1, -1);
		m_state.sector_action = SpecialSectorType.None;
	}

	private void InternalMoveOn(Hexagon.Position position)
	{
		m_state.players[m_state.player_turn].position = position;
		m_state.player_turn = (m_state.player_turn + 1) % m_state.players.Length;
		m_state.turn_count++;
		SetupNewTurn();
	}

	public void TurnSpecialSectorMoveOn(Hexagon.Position position)
	{
		StringBuilder builder = new StringBuilder();
		builder.Append("[P");
		builder.Append((m_state.player_turn + 1).ToString());
		builder.Append(":T");
		builder.Append((m_state.turn_count / m_state.players.Length) + 1);
		builder.Append("] ");

		switch(m_state.sector_action)
		{
			case SpecialSectorType.None:
				break;
			case SpecialSectorType.NoiseInYourSector:
				builder.Append("Noise in sector " + m_state.position_to_move.ToString());
				break;
			case SpecialSectorType.NoiseInAnySector:
				builder.Append("Noise in sector " + position.ToString());
				break;
			case SpecialSectorType.SilenceInAllSectors:
				builder.Append("Silence in all sectors");
				break;
		}

		PrintLog(builder.ToString());
		InternalMoveOn(m_state.position_to_move);
	}

	public void TurnMoveOn(Hexagon.Position position)
	{
		if (m_state.sector_action != SpecialSectorType.None)
			return;

		if (m_currentWalkablePositions.Contains(position))
		{
			Hexagon.Type type = (Hexagon.Type) typemat[position.y, position.x];
			switch(type)
			{
				case Hexagon.Type.CleanSector:
					break;
				case Hexagon.Type.SpecialSector:
					m_state.position_to_move = position;
					int rand = m_random.Next(5);
					if (rand == 0 || rand == 1)
						m_state.sector_action = SpecialSectorType.NoiseInYourSector;
					else if (rand == 2 || rand == 3)
						m_state.sector_action = SpecialSectorType.NoiseInAnySector;
					else
						m_state.sector_action = SpecialSectorType.SilenceInAllSectors;
					break;
			}
			

			if (type == Hexagon.Type.CleanSector)
			{
				StringBuilder builder = new StringBuilder();
				builder.Append("[P");
				builder.Append((m_state.player_turn + 1).ToString());
				builder.Append(":T");
				builder.Append((m_state.turn_count / m_state.players.Length) + 1);
				builder.Append("] ");
				builder.Append("Moved to safe sector.");

				PrintLog(builder.ToString());
				InternalMoveOn(position);
			}
			else
			{
				bool interactable = (m_state.sector_action == SpecialSectorType.NoiseInAnySector);
				m_click = new Hexagon.Position(-1, -1);
				Hexagon.Position pos = new Hexagon.Position(0, 0);
				for (pos.y = 0; pos.y < m_size_y; ++pos.y)
					for (pos.x = 0; pos.x < m_size_x; ++pos.x)
						if (IsWalkeable(pos))
							cells[pos.y, pos.x].interactable = interactable;

				if (m_state.sector_action == SpecialSectorType.NoiseInYourSector)
					cells[m_state.position_to_move.y, m_state.position_to_move.x].interactable = true;
			}
		}
	}

	public void TurnSpecialSectorMove()
	{
		TurnSpecialSectorMoveOn(m_click);
	}

	public void TurnMove()
	{
		TurnMoveOn(m_click);
	}

	
	public void ClickOn(Hexagon.Position position)
	{
		m_click = position;
	}

}
