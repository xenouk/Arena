using UnityEngine;

public class UIDirectionControl : MonoBehaviour {
	public bool m_UseRelativePosition = true;
	public bool m_UseRelativeRotation = true;

	private Vector3 m_RelativePosition;
	private Quaternion m_RelativeRotation;

	private void Start() {
		m_RelativePosition = transform.localPosition;
		m_RelativeRotation = transform.localRotation;
	}

	private void Update() {
		if (m_UseRelativeRotation)
			transform.rotation = m_RelativeRotation;

		if (m_UseRelativePosition)
			transform.position = transform.parent.position + m_RelativePosition;
	}
}