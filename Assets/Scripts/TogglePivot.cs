using UnityEngine;
using System.Collections;

public class TogglePivot : MonoBehaviour
{
	public Vector2 ToggledPosition;
	public Vector2 UntoggledPosition;
	public bool Toggled = false;
	public float BlendSpeed = 0.1f;

	private Vector2 m_currentPosition;
	private RectTransform m_rect;

	public void Toggle()
	{
		Toggled = !Toggled;
	}

	virtual protected void Start()
	{
		m_rect = GetComponent<RectTransform>();

		if (Toggled)
			m_currentPosition = ToggledPosition;
		else
			m_currentPosition = UntoggledPosition;
	}
	
	virtual protected void FixedUpdate ()
	{
		if (Toggled)
			m_currentPosition = Vector2.Lerp(m_currentPosition, ToggledPosition, BlendSpeed);
		else
			m_currentPosition = Vector2.Lerp(m_currentPosition, UntoggledPosition, BlendSpeed);

		if (m_rect == null)
			return;

		m_rect.pivot = m_currentPosition;
	}
}
