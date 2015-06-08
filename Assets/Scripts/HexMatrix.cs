using UnityEngine;
using System.Collections;

public class HexMatrix : MonoBehaviour {

	private int [,] typemat = new int[,]
	{
		{1,1,1,1,1,1,1,1,1,1},
		{0,0,0,0,0,0,0,0,0,0},
		{0,2,0,0,0,0,0,0,0,0},
		{0,0,0,0,0,0,0,0,0,0},
		{0,0,0,0,0,0,0,0,0,0},
		{0,0,0,0,0,0,2,2,0,0},
		{0,0,0,0,0,0,2,0,0,0},
		{0,0,0,0,0,0,0,0,0,0},
		{0,0,0,0,0,0,0,0,0,0},
		{0,0,0,0,0,0,0,0,0,0}
	};

	private Hexagon[,] cells;

	public GameObject square;
	public Vector2 stride = new Vector2(1.70f, 1.50f);

	struct State
	{
		struct Player
		{
			enum Type
			{
				Human,
				Alien
			}


		}
	}

	private int size = 0;
	private Vector2 humanPos = Vector2.zero;
	private Vector2 alienPos = Vector2.zero;
	private bool alienTurn = false;


	// Use this for initialization
	void Start ()
	{
		size = (int) Mathf.Sqrt(typemat.Length);
		cells = new Hexagon[size, size];

		Vector2 halfsize = stride * size * -0.5f;
		for(int y = 0; y < size; ++y) {
			for(int x = 0; x < size; ++x)
			{
				GameObject mesh = (GameObject) Instantiate(square);
				mesh.name = "Cell" + x + "x" + y;
				mesh.transform.SetParent (transform, false);
				mesh.transform.position = new Vector3(x * stride.x + halfsize.x + (y % 2) * 0.5f * -stride.x, 0, y * stride.y + halfsize.y);

				Hexagon cell = mesh.GetComponent<Hexagon>();
				cells[y, x] = mesh.GetComponent<Hexagon>();
				cell.Init(this, new Hexagon.Position(x, y), (Hexagon.Type) typemat[y, x]);
				cell.enabled = true;
			}
		}

		humanPos = new Vector2 (3, 2);
		alienPos = new Vector2 (4, 4);
	}

	public void ClickOn(Hexagon.Position position)
	{
		Debug.Log(position.x + " " + position.y);
	}

}
