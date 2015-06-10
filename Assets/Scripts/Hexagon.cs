using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class Hexagon : Selectable {

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
			for (;;)
			{
				int next = ix / lut.Length;
				int curr = ix % lut.Length;
				name += lut[curr];
				if (next == 0)
					break;

				ix = next;
			}

			name += (y+1).ToString("D2");
			return name;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Position)) return false;

			Position p = (Position) obj;
			return p.x == x && p.y == y;
		}

		public override int GetHashCode()
		{
			return (((x << 5) + x) ^ y);
		}

		public int CompareTo(object obj)
		{
			if (!(obj is Position)) return -1;

			Position p = (Position) obj;
			int compare_x = x.CompareTo(p.x);
			return compare_x == 0 ? y.CompareTo(p.y) : compare_x;
		}
	}

	public enum Type
	{
		CleanSector = 0,
		SpecialSector = 1,
		Hollow = 2,
		HumanSpawn = 3,
		AlienSpawn = 4,
		EscapeHatch = 5,
		Count = 6
	}

	public enum Highlight
	{
		None = 0,
		Current = 1,
	}

	public TextMesh m_hexText;
	private HexMatrix m_parent;
	private Position m_pos;
	private Type m_type = Type.Hollow;

	[SerializeField]
	private Highlight m_highlight = Highlight.None;
	public Highlight highlight { get { return m_highlight; } set { m_highlight = value; OnSetProperty(); } }

	private Renderer m_childRenderer = null;

	void Awake ()
	{
		Transform childTransform = transform.Find("Mesh");
		if (childTransform != null)
		{
			m_childRenderer = childTransform.GetComponent<Renderer>();
		}

		enabled = false;
	}

	public void Init(HexMatrix parent, Position position, Type type, Texture texture, string hexText)
	{
		m_parent = parent;
		m_pos = position;
		m_type = (Type) type;

		if (m_type == Type.Hollow)
		{
			interactable = false;
			if (m_childRenderer != null)
				m_childRenderer.enabled = false;
		}
		else if (m_childRenderer != null)
			m_childRenderer.material.mainTexture = texture;

		if (m_hexText != null)
		{
			m_hexText.text = hexText;
		}

		

		enabled = true;
	}

	private Color m_typeColor = Color.black;
	private Color m_currentColor = Color.black;
	private Color m_targetColor = Color.black;

	protected void DoColorTransition(Color targetColor, bool instant)
	{
		m_targetColor = targetColor;

		if (instant)
			m_currentColor = targetColor;
	}

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		m_typeColor = Color.white;
		if (m_highlight == Highlight.Current)
			m_typeColor = Color.cyan;

		switch (state)
		{
			case SelectionState.Normal:
				DoColorTransition(m_typeColor, instant);
				break; 
			case SelectionState.Highlighted:
				DoColorTransition(Color.Lerp(Color.red, m_typeColor, 0.5f), instant);
				break;
			case SelectionState.Pressed:
				DoColorTransition(Color.Lerp(Color.yellow, m_typeColor, 0.5f), instant);
				m_parent.ClickOn(m_pos);
				break;
			case SelectionState.Disabled:
				DoColorTransition(m_typeColor * 0.5f, instant);
				break;
			default:
				break;
		}
	}

	public void FixedUpdate()
	{
		m_currentColor = Color.Lerp(m_currentColor, m_targetColor, 0.25f);
	}

	public void Update()
	{
		if (m_childRenderer != null)
		{
			Material mat = m_childRenderer.material;
			mat.color = m_currentColor;
		}

		if (m_hexText != null)
		{
			Color textColor = m_currentColor * 0.25f;
			textColor.a = 1.0f;
			m_hexText.color = textColor;
		}
	}
}
