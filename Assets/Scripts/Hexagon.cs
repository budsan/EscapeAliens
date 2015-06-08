using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class Hexagon : Selectable {

	public struct Position
	{
		public int x;
		public int y;

		public Position(int _x, int _y)
		{
			x = _x;
			y = _y;
		}
	}

	public enum Type
	{
		Safe = 0,
		Danger = 1,
		None = 2,
		Length = 3		
	}

	public enum Highlight
	{
		None = 0,
		Current = 1,
		Reachable = 2,
		Unreachable = 3,
	}

	private HexMatrix m_parent;
	private Position m_pos;
	private Type m_type = Type.None;
	private Highlight m_highlight = Highlight.None;
	private Renderer m_childRenderer = null;

	static private Color[] TypeColor =
	{
		Color.white,
		Color.gray,
		Color.black,
		Color.gray
	};

	static private Color[] HighlightColor =
	{
		Color.white,
		Color.gray,
		Color.black,
		Color.gray
	};

	void Awake ()
	{
		Transform childTransform = transform.Find("Mesh");
		if (childTransform != null)
		{
			m_childRenderer = childTransform.GetComponent<Renderer>();
		}

		enabled = false;
	}

	public void Init(HexMatrix parent, Position position, Type type)
	{
		m_parent = parent;
		m_pos = position;
		m_type = (Type) type;

		if (m_type == Type.None)
		{
			Interactable = false;
			if (m_childRenderer != null)
			{
				m_childRenderer.enabled = false;
			}
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

	protected bool m_lastIsPointerDown = false;
	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		m_typeColor = TypeColor[(int) m_type];
		switch (state)
		{
			case SelectionState.Normal:
				DoColorTransition(m_typeColor, instant);
				break; 
			case SelectionState.Highlighted:
				DoColorTransition(Color.Lerp(Color.red, m_typeColor, 0.5f), instant);
				if (m_lastIsPointerDown && !isPointerDown)
					m_parent.ClickOn(m_pos);

				m_lastIsPointerDown = false;
				break;
			case SelectionState.Pressed:
				DoColorTransition(Color.Lerp(Color.yellow, m_typeColor, 0.5f), instant);
				m_lastIsPointerDown = isPointerDown;
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
	}
}
