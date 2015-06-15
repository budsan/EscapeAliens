using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class HexMatrix : MonoBehaviour, GameStateListener {

	// Galilei
	// http://www.escapefromthealiensinouterspace.com/imgs/maps/galilei.jpg
	private int[,] map = new int[,]
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

	private Hexagon[,] cells = null;
	private GameState m_state = new GameState();

	private bool m_newTurn = false;
	public bool NewTurn { get { return m_newTurn; } }

	private GameState.Position m_click = new GameState.Position(-1, -1);
	private GameState.Position m_highlighted = new GameState.Position(-1, -1);

	public bool ValidSectorSelected()
	{
		return m_state.IsWalkeable(m_click);
	}

	public int CurrentRound()
	{
		return m_state.CurrentRound;
	}

	public int PlayerTurn()
	{
		return m_state.PlayerTurn;
	}

	public int CurrentWinner()
	{
		return m_state.CurrentWinner;
	}

	public bool IsHumanPlaying()
	{
		try
		{
			return m_state.CurrentPlayer.type == GameState.Player.Type.Human;
		}
		catch
		{
			return false;
		}
	}

	public GameState.SectorState CurrentSectorState()
	{
		return m_state.CurrentSectorState;
	}

	public GameState.Position GetCurrentPlayerPosition()
	{
		return m_state.CurrentPlayer.position;
	}

	void Start ()
	{
		int seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
		m_state = new GameState();
		m_state.AddListener(this);
		m_state.Start(seed, numPlayers, map);
	}

	public void OnStartEnd()
	{
		cells = new Hexagon[GameState.map_size_y, GameState.map_size_x];
		if (material.Length < (int)GameState.CellType.Count)
		{
			Debug.LogError("You must set at least " + (int)GameState.CellType.Count + " textures.");
			return;
		}

		for (int i = 0; i < (int)GameState.CellType.Count; i++)
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

		Vector2 halfsize = new Vector2(stride.x * GameState.map_size_x, stride.y * GameState.map_size_y) * -0.5f;
		for (int y = 0; y < GameState.map_size_y; ++y)
		{
			for (int x = 0; x < GameState.map_size_x; ++x)
			{
				GameState.Position hexpos = new GameState.Position(x, y);
				GameState.CellType hexType = m_state.GetMapCell(hexpos);
				string hexName = "";
				switch (hexType)
				{
					case GameState.CellType.CleanSector:
						hexName = hexpos.ToString();
						break;
					case GameState.CellType.SpecialSector:
						hexName = hexpos.ToString();
						break;
					case GameState.CellType.EscapeHatch:
						GameState.Hatch hatch = new GameState.Hatch();
						if (m_state.GetHatch(hexpos, out hatch))
							hexName = hatch.Id.ToString();
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
					mesh = (GameObject)Instantiate(HexagonPrefab);

				mesh.name = "Hex " + hexpos.ToString();
				mesh.transform.SetParent(transform, false);
				mesh.transform.SetAsLastSibling();
				mesh.transform.localPosition = new Vector3(x * stride.x + halfsize.x, 0, -y * stride.y - halfsize.y + (x % 2) * 0.5f * -stride.y);

				Hexagon cell = mesh.GetComponent<Hexagon>();
				cells[y, x] = mesh.GetComponent<Hexagon>();

				cell.Init(this, hexpos, hexType, material[(int)hexType], hexName);
				cell.enabled = true;
				cell.interactable = false;
			}
		}
	}

	public void OnLogChanged()
	{
		if (LogText != null)
			LogText.text = m_state.Log;
	}

	public void OnNewTurn()
	{
		GameState.Position pos = new GameState.Position(0, 0);
		for (pos.y = 0; pos.y < GameState.map_size_y; ++pos.y)
			for (pos.x = 0; pos.x < GameState.map_size_x; ++pos.x)
			{
				cells[pos.y, pos.x].highlight = Hexagon.Highlight.None;
				cells[pos.y, pos.x].interactable = false;
			}

		m_newTurn = true;
	}

	public void TurnSpecialSectorMove()
	{
		m_state.TurnSpecialSectorMoveOn(m_click);
	}

	public void SetupNewTurn()
	{
		if (!m_newTurn)
			return;

		if (m_state.CurrentWinner != GameState.Winner.None)
			return;

		GameState.Player currentPlayer = m_state.CurrentPlayer;
		m_highlighted = currentPlayer.position;
		cells[m_highlighted.y, m_highlighted.x].highlight = Hexagon.Highlight.Current;

		foreach (GameState.Position position in m_state.CurrentWalkablePositions)
			cells[position.y, position.x].interactable = true;

		m_click = new GameState.Position(-1, -1);

		m_newTurn = false;
	}

	public void TurnAttack()
	{
		if (m_click.x < 0)
			return;

		m_state.TurnAttackOn(m_click);
	}

	public void TurnMove()
	{
		if (m_click.x < 0)
			return;

		m_state.TurnMoveOn(m_click);
		if (m_state.CurrentSectorState != GameState.SectorState.None)
		{
			bool interactable = (m_state.CurrentSectorState == GameState.SectorState.OnNoiseInAnySector);

			m_click = new GameState.Position(-1, -1);
			GameState.Position pos = new GameState.Position(0, 0);
			for (pos.y = 0; pos.y < GameState.map_size_y; ++pos.y)
				for (pos.x = 0; pos.x < GameState.map_size_x; ++pos.x)
					if (m_state.IsWalkeable(pos))
						cells[pos.y, pos.x].interactable = interactable;

			pos = m_state.CurrentSectorStatePosition;
			if (m_state.CurrentSectorState == GameState.SectorState.OnNoiseInYourSector)
				cells[pos.y, pos.x].interactable = true;
		}
	}

	public void ClickOn(GameState.Position position)
	{
		m_click = position;
	}

}
