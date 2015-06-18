using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class Hexagon : Selectable {

	public enum Highlight
	{
		None = 0,
		Current = 1,
	}

	[SerializeField]
	public TextMesh m_hexText;

	[SerializeField]
	private HexMatrix m_parent;

	[SerializeField]
	private GameState.Position m_pos;

	[SerializeField]
	private GameState.CellType m_type = GameState.CellType.Hollow;

	[SerializeField]
	private Highlight m_highlight = Highlight.None;
	public Highlight highlight { get { return m_highlight; } set { m_highlight = value; OnSetProperty(); } }

	[SerializeField]
	private Renderer m_childRenderer = null;

	void Awake ()
	{
		Transform childTransform = transform.Find("Mesh");
		if (childTransform != null)
		{
			m_childRenderer = childTransform.GetComponent<Renderer>();
		}
	}

	public void Init(HexMatrix parent, GameState.Position position, GameState.CellType type, Material material, string hexText)
	{
		Transform childTransform = transform.Find("Mesh");
		if (m_childRenderer == null && childTransform != null)
			m_childRenderer = childTransform.GetComponent<Renderer>();

		m_parent = parent;
		m_pos = position;
		m_type = (GameState.CellType) type;

		if (m_type == GameState.CellType.Hollow)
		{
			interactable = false;
			if (m_childRenderer != null)
				m_childRenderer.enabled = false;

			if (m_hexText != null)
				m_hexText.GetComponent<Renderer>().enabled = false;
		}
			
		if(m_childRenderer != null)
			m_childRenderer.material = material;

		OnSetProperty();

		if (m_hexText != null)
		{
			m_hexText.text = hexText;
			Color textColor = m_currentColor * 0.25f;
			textColor.a = 1.0f;
			m_hexText.color = textColor;
		}	
	}

	[SerializeField]
	private Color m_typeColor = Color.black;

	[SerializeField]
	private Color m_currentColor = Color.black;

	[SerializeField]
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

	/*
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
	*/
}
