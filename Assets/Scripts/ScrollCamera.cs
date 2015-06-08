using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
using System;

public class ScrollCamera : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
{
	[Serializable]
	public class ScrollCameraEvent : UnityEvent<Vector2> { }

	[SerializeField]
	private Transform m_Target;
	public Transform target { get { return m_Target; } set { m_Target = value; } }
	private Vector2 TargetPosition
	{
		get { return new Vector2( m_Target.transform.position.x / m_TargetScale, m_Target.transform.position.z / m_TargetScale); }
		set { m_Target.transform.position = new Vector3(value.x * m_TargetScale, m_Target.transform.position.y, value.y * m_TargetScale); }
	}

	[SerializeField]
	private float m_TargetScale = 0.1f;
	public float targetScale { get { return m_TargetScale; } set { m_TargetScale = value; } }

	[SerializeField]
	private bool m_Inertia = true;
	public bool inertia { get { return m_Inertia; } set { m_Inertia = value; } }

	[SerializeField]
	private float m_DecelerationRate = 0.135f; // Only used when inertia is enabled
	public float decelerationRate { get { return m_DecelerationRate; } set { m_DecelerationRate = value; } }

	[SerializeField]
	private float m_ScrollSensitivity = 1.0f;
	public float scrollSensitivity { get { return m_ScrollSensitivity; } set { m_ScrollSensitivity = value; } }

	[SerializeField]
	private ScrollCameraEvent m_OnValueChanged = new ScrollCameraEvent();
	public ScrollCameraEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

	// The offset from handle position to mouse down position
	private Vector2 m_PointerStartLocalCursor = Vector2.zero;
	private Vector2 m_ContentStartPosition = Vector2.zero;

	private Vector2 m_Velocity;
	public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

	private bool m_Dragging;

	private Vector2 m_PrevPosition = Vector2.zero;

	protected ScrollCamera()
	{ }

	protected virtual void OnEnable()
	{
	}

	protected virtual void OnDisable()
	{
	}

	public virtual bool IsActive()
	{
		return enabled && isActiveAndEnabled && gameObject.activeInHierarchy && m_Target != null;
	}

	public virtual void StopMovement()
	{
		m_Velocity = Vector2.zero;
	}

	public virtual void OnScroll(PointerEventData data)
	{
		if (!IsActive())
			return;

		Vector2 delta = data.scrollDelta;
		// Down is positive for scroll events, while in UI system up is positive.
		delta.y *= -1;
		Vector2 position = TargetPosition;
		position += delta * m_ScrollSensitivity;
		SetContentAnchoredPosition(position);
	}

	public virtual void OnInitializePotentialDrag(PointerEventData eventData)
	{
		m_Velocity = Vector2.zero;
	}

	public virtual void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (!IsActive())
			return;

		Plane plane = new Plane(transform.up, transform.position);
		Ray ray = eventData.pressEventCamera.ScreenPointToRay(eventData.position);
		float rayDistance = eventData.pressEventCamera.farClipPlane;
		if (!plane.Raycast(ray, out rayDistance))
			return;

		Vector3 rayHit = ray.GetPoint(rayDistance);
		m_PointerStartLocalCursor = new Vector2(rayHit.x, rayHit.z);
		m_ContentStartPosition = TargetPosition;
		m_Dragging = true;
	}

	public virtual void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		m_Dragging = false;
	}

	public virtual void OnDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (!IsActive())
			return;

		Plane plane = new Plane(transform.up, transform.position);
		Ray ray = eventData.pressEventCamera.ScreenPointToRay(eventData.position);
		float rayDistance =  eventData.pressEventCamera.farClipPlane;
		if (!plane.Raycast(ray, out rayDistance))
			return;

		Vector3 rayHit = ray.GetPoint(rayDistance);
		Vector2 localCursor = new Vector2(rayHit.x, rayHit.z);
		var pointerDelta = localCursor - m_PointerStartLocalCursor;
		Vector2 position = m_ContentStartPosition + pointerDelta;

		// Offset to get content into place in the view.
		Vector2 offset = position - TargetPosition;
		position += offset;
		SetContentAnchoredPosition(position);
	}

	protected virtual void SetContentAnchoredPosition(Vector2 position)
	{
		TargetPosition = position;
	}

	protected virtual void LateUpdate()
	{
		if (!m_Target)
			return;

		float deltaTime = Time.unscaledDeltaTime;
		if (!m_Dragging && m_Velocity != Vector2.zero)
		{
			Vector2 position = TargetPosition;
			for (int axis = 0; axis < 2; axis++)
			{
				// move content according to velocity with deceleration applied.
				if (m_Inertia)
				{
					m_Velocity[axis] *= Mathf.Pow(m_DecelerationRate, deltaTime);
					if (Mathf.Abs(m_Velocity[axis]) < 1)
						m_Velocity[axis] = 0;
					position[axis] += m_Velocity[axis] * deltaTime;
				}
				// If we have neither elaticity or friction, there shouldn't be any velocity.
				else
				{
					m_Velocity[axis] = 0;
				}
			}

			if (m_Velocity != Vector2.zero)
			{
				SetContentAnchoredPosition(position);
			}
		}

		if (m_Dragging && m_Inertia)
		{
			Vector3 newVelocity = (TargetPosition - m_PrevPosition) / (deltaTime * targetScale);
			m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime);
		}

		if (TargetPosition != m_PrevPosition)
		{
			m_OnValueChanged.Invoke(normalizedPosition);
			UpdatePrevData();
		}
	}

	private void UpdatePrevData()
	{
		if (m_Target == null)
			m_PrevPosition = Vector2.zero;
		else
			m_PrevPosition = TargetPosition;
	}


	public Vector2 normalizedPosition
	{
		get
		{
			return new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition);
		}
		set
		{
			SetNormalizedPosition(value.x, 0);
			SetNormalizedPosition(value.y, 1);
		}
	}

	public float horizontalNormalizedPosition
	{
		get
		{
			return m_Target.localPosition.x;
		}
		set
		{
			SetNormalizedPosition(value, 0);
		}
	}

	public float verticalNormalizedPosition
	{
		get
		{
			return m_Target.localPosition.z;
		}
		set
		{
			SetNormalizedPosition(value, 1);
		}
	}

	private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
	private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 1); }

	private void SetNormalizedPosition(float value, int axis)
	{
		axis = (axis == 1) ? 2 : axis;

		// The new content localPosition, in the space of the view.
		float newLocalPosition = m_Target.localPosition[axis];

		Vector3 localPosition = m_Target.localPosition;
		if (Mathf.Abs(localPosition[axis] - newLocalPosition) > 0.01f)
		{
			localPosition[axis] = newLocalPosition;
			m_Target.localPosition = localPosition;
			m_Velocity[axis] = 0;
		}
	}

	private static float RubberDelta(float overStretching, float viewSize)
	{
		return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
	}
}
