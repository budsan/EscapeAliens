using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HexMatrix : MonoBehaviour {

	private int [,] typemat = new int[,]
	{
		{2,1,2,0,0,0,0,0,2,1,2},
		{1,5,1,0,1,1,1,0,1,5,1},
		{1,1,1,0,0,0,0,0,1,1,1},
		{0,0,0,0,1,0,1,0,0,0,0},
		{0,0,0,0,0,4,0,0,0,0,0},
		{2,0,1,2,2,2,2,2,1,0,2},
		{0,1,0,0,0,3,0,0,0,1,0},
		{0,0,0,0,0,0,0,0,0,0,0},
		{0,1,0,0,1,1,1,0,0,1,0},
		{1,5,1,0,0,0,0,0,1,5,1},
		{1,1,1,0,1,1,1,0,1,1,1}
	};

	private Hexagon[,] cells;

	public GameObject square;
	public Vector2 stride = new Vector2(1.50f, 1.70f);
	public Texture[] textures;

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
		}

		public Player[] players;
		public int player_turn;
	}

	private int size = 0;
	private GameState currentState;

	// Use this for initialization
	void Start ()
	{
		size = (int) Mathf.Sqrt(typemat.Length);
		cells = new Hexagon[size, size];

		if (textures.Length < (int) Hexagon.Type.Count)
		{
			Debug.LogError("You must set at least " + (int)Hexagon.Type.Count + " textures.");
			return;
		}

		for (int i = 0; i < (int) Hexagon.Type.Count; i++)
		{
			if (textures[i] == null)
			{
				Debug.LogError("You must set all textures. Your texture " + i + " isn't set.");
				return;
			}
		}

		Vector2 halfsize = stride * size * -0.5f;
		Hexagon.Position AlienSpawnPosition = new Hexagon.Position(-1, -1);
		Hexagon.Position HumanSpawnPosition = new Hexagon.Position(-1, -1);

		int hatchNum = 0;
		for(int y = 0; y < size; ++y) {
			for(int x = 0; x < size; ++x)
			{
				GameObject mesh = (GameObject) Instantiate(square);
				Hexagon.Position hexpos = new Hexagon.Position(x, y);
				
				mesh.name = "Hex " + hexpos.ToString();
				mesh.transform.SetParent (transform, false);
				mesh.transform.localPosition = new Vector3(x * stride.x + halfsize.x, 0, -y * stride.y - halfsize.y + (x % 2) * 0.5f * -stride.y);

				Hexagon cell = mesh.GetComponent<Hexagon>();
				cells[y, x] = mesh.GetComponent<Hexagon>();
				int type = typemat[y, x];
				if (type < (int) Hexagon.Type.Count)
				{
					string hexName = ""; 
					Hexagon.Type hexType = (Hexagon.Type) type;
					switch(hexType)
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

					cell.Init(this, hexpos, (Hexagon.Type)type, textures[type], hexName);
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

		currentState = new GameState();
		currentState.player_turn = 0;
		currentState.players = new GameState.Player[2];
		currentState.players[0] = new GameState.Player();
		currentState.players[0].type = GameState.Player.Type.Human;
		currentState.players[0].position = HumanSpawnPosition;

		currentState.players[1] = new GameState.Player();
		currentState.players[1].type = GameState.Player.Type.Alien;
		currentState.players[1].position = AlienSpawnPosition;

		SetupNewTurn();
	}

	private bool IsWalkeable(Hexagon.Position position)
	{
		return position.x >= 0
			&& position.x < size
			&& position.y >= 0
			&& position.y < size 
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

	private HashSet<Hexagon.Position> m_currentWalkablePositions = new HashSet<Hexagon.Position>();
	private Hexagon.Position highlighted = new Hexagon.Position(-1, -1);
	private void SetupNewTurn()
	{
		if (highlighted.x >= 0)
			cells[highlighted.y, highlighted.x].highlight = Hexagon.Highlight.None;

		foreach (Hexagon.Position position in m_currentWalkablePositions)
			cells[position.y, position.x].interactable = false;

		GameState.Player currentPlayer = currentState.players[currentState.player_turn];
		Debug.Log("Current player turn: " + currentState.player_turn + ". CurrentPlayer: " + currentPlayer.position.ToString() + " " + currentPlayer.type.ToString());

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
		highlighted = currentPlayer.position;
		cells[highlighted.y, highlighted.x].highlight = Hexagon.Highlight.Current;

		foreach (Hexagon.Position position in m_currentWalkablePositions)
			cells[position.y, position.x].interactable = true;

	}

	public void TurnMoveOn(Hexagon.Position position)
	{
		if (m_currentWalkablePositions.Contains(position))
		{
			currentState.players[currentState.player_turn].position = position;
			currentState.player_turn = (currentState.player_turn + 1) % currentState.players.Length;
			SetupNewTurn();
		}
	}

	Hexagon.Position m_click = new Hexagon.Position(-1, -1);
	public void ClickOn(Hexagon.Position position)
	{
		if (position.Equals(m_click))
		{
			m_click = new Hexagon.Position(-1, -1);
			TurnMoveOn(position);
			m_click = new Hexagon.Position(-1, -1);
		}
		else 
			m_click = position;
	}

}
