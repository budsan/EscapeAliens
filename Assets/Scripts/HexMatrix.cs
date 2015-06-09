using UnityEngine;
using System.Collections;

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
							break;
						case Hexagon.Type.AlienSpawn:
							break;
						case Hexagon.Type.EscapeHatch:
							hexName = (++hatchNum).ToString();
							break;
						default:
							break;
					}

					cell.Init(this, hexpos, (Hexagon.Type)type, textures[type], hexName);
					cell.enabled = true;
				}
			}
		}

		currentState = new GameState();
		currentState.player_turn = 0;
		currentState.players = new GameState.Player[2];
		currentState.players[0] = new GameState.Player();
		currentState.players[0].type = GameState.Player.Type.Human;
	}

	public void ClickOn(Hexagon.Position position)
	{
		Debug.Log(position.x + " " + position.y);
	}

}
