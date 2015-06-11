using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
using System;

public class ScrollCamera : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
{
	[Serializable]
	public class ScrollCameraEvent : UnityEvent<Vector3> { }

	private Transform m_target = null;

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
	private Vector3 m_PointerStartLocalCursor = Vector3.zero;
	private Vector3 m_ContentStartPosition = Vector3.zero;

	private Vector3 m_Velocity;
	public Vector3 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

	private bool m_Dragging;

	private Vector3 m_PrevPosition = Vector3.zero;

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
		return enabled && isActiveAndEnabled && gameObject.activeInHierarchy && m_target != null;
	}

	public virtual void StopMovement()
	{
		m_Velocity = Vector3.zero;
	}

	public virtual void OnScroll(PointerEventData data)
	{
		m_target = data.pressEventCamera != null ? data.pressEventCamera.transform : null;

		if (!IsActive())
			return;

		Vector3 delta = m_target.right * data.scrollDelta.x + m_target.forward * data.scrollDelta.y;
		Vector3 position = m_target.position;
		position += delta * m_ScrollSensitivity;
		m_target.position = position;
	}

	public virtual void OnInitializePotentialDrag(PointerEventData eventData)
	{
		m_target = eventData.pressEventCamera.transform;
		m_Velocity = Vector3.zero;
	}

	public virtual void OnBeginDrag(PointerEventData eventData)
	{
		m_target = eventData.pressEventCamera != null ? eventData.pressEventCamera.transform : null;
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (!IsActive())
			return;

		Camera camera = eventData.pressEventCamera;
		Plane plane = new Plane(transform.up, transform.position);
		Ray ray = camera.ScreenPointToRay(eventData.position);
		float rayDistance = eventData.pressEventCamera.farClipPlane;
		if (!plane.Raycast(ray, out rayDistance))
			return;

		Vector3 rayHit = ray.GetPoint(rayDistance);
		m_PointerStartLocalCursor = rayHit;
		m_ContentStartPosition = m_target.position;
		m_Dragging = true;
	}

	public virtual void OnEndDrag(PointerEventData eventData)
	{
		m_target = eventData.pressEventCamera != null ? eventData.pressEventCamera.transform : null;
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		m_Dragging = false;
	}

	public virtual void OnDrag(PointerEventData eventData)
	{
		m_target = eventData.pressEventCamera != null ? eventData.pressEventCamera.transform : null;
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (!IsActive())
			return;

		Camera camera = eventData.pressEventCamera;
		Vector3 contentCurrentPosition = m_target.position;
		camera.transform.position = m_ContentStartPosition;

		Plane plane = new Plane(transform.up, transform.position);
		Ray ray = camera.ScreenPointToRay(eventData.position);
		
		float rayDistance =  eventData.pressEventCamera.farClipPlane;
		if (!plane.Raycast(ray, out rayDistance))
			return;

		Vector3 rayHit = ray.GetPoint(rayDistance);
		var pointerDelta = m_PointerStartLocalCursor - rayHit;

		Vector3 position = m_ContentStartPosition + pointerDelta;
		

		// Offset to get content into place in the view.
		Vector3 offset = position - m_target.position;
		position += offset;
		SetContentAnchoredPosition(position);
	}

	protected virtual void SetContentAnchoredPosition(Vector3 position)
	{
		m_target.position = position;
	}

	protected virtual void LateUpdate()
	{
		if (m_target == null)
			return;

		float deltaTime = Time.unscaledDeltaTime;
		if (!m_Dragging && m_Velocity != Vector3.zero)
		{
			Vector3 position = m_target.position;
			for (int axis = 0; axis < 3; axis++)
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

			if (m_Velocity != Vector3.zero)
			{
				SetContentAnchoredPosition(position);
			}
		}

		if (m_Dragging && m_Inertia)
		{
			Vector3 newVelocity = (m_target.position - m_PrevPosition) / (deltaTime * targetScale);
			m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime);
		}

		if (m_target.position != m_PrevPosition)
		{
			m_OnValueChanged.Invoke(normalizedPosition);
			UpdatePrevData();
		}
	}

	private void UpdatePrevData()
	{
		if (m_target == null)
			m_PrevPosition = Vector3.zero;
		else
			m_PrevPosition = m_target.position;
	}


	public Vector3 normalizedPosition
	{
		get
		{
			return new Vector3(horizontalNormalizedPosition, verticalNormalizedPosition);
		}
		set
		{
			SetNormalizedPosition(value.x, 0);
			SetNormalizedPosition(value.y, 1);
			SetNormalizedPosition(value.z, 2);
		}
	}

	public float horizontalNormalizedPosition
	{
		get
		{
			return m_target.localPosition.x;
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
			return m_target.localPosition.z;
		}
		set
		{
			SetNormalizedPosition(value, 2);
		}
	}

	private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
	private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 2); }

	private void SetNormalizedPosition(float value, int axis)
	{
		// The new content localPosition, in the space of the view.
		float newLocalPosition = m_target.localPosition[axis];

		Vector3 localPosition = m_target.localPosition;
		if (Mathf.Abs(localPosition[axis] - newLocalPosition) > 0.01f)
		{
			localPosition[axis] = newLocalPosition;
			m_target.localPosition = localPosition;
			m_Velocity[axis] = 0;
		}
	}

	private static float RubberDelta(float overStretching, float viewSize)
	{
		return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
	}
}
