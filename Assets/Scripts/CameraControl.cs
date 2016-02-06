using UnityEngine;
using System.Collections.Generic;


public class CameraControl : MonoBehaviour {
	static public CameraControl sInstance;

	public float m_DampTime = 0.2f;                   // Approximate time for the camera to refocus.
	public float m_ScreenEdgeBuffer = 4f;             // Space between the top/bottom most target and the screen edge (multiplied by aspect for left and right).
	public float m_MaxSize = 8f;                    // The smallest orthographic size the camera can be.
	public float m_ConvertDistanceToSize = 1;      // Used to multiply by the offset of the rig to the furthest target.

	private Camera[] m_Camera;                      // Used for referencing the camera.
	private float m_ZoomSpeed;                      // Reference speed for the smooth damping of the orthographic size.
	private Vector3 m_MoveVelocity;                 // Reference velocity for the smooth damping of the position.



	private void Awake() {
		sInstance = this;
		m_Camera = GetComponentsInChildren<Camera> ();
	}

	private void FixedUpdate() {
		// The camera is moved towards a target position which is returned.
		Vector3 targetPosition = Move ();

		// The size is changed based on where the camera is going to be.
		Zoom (targetPosition);
	}


	private Vector3 Move() {
		// Find the average position of the targets and smoothly transition to that position.
		Vector3 targetPosition = FindAveragePosition ();
		transform.position = Vector3.SmoothDamp (transform.position, targetPosition, ref m_MoveVelocity, m_DampTime);

		return targetPosition;
	}


	private Vector3 FindAveragePosition() {
		Vector3 average = new Vector3 ();
		int numTargets = 0;

		// Go through all the targets and add their positions together.
		for (int i = 0; i < GameManager.m_Players.Count; i++) {
			// If the target isn't active, go on to the next one.
			if (!GameManager.m_Players [i].m_Instance.activeSelf)
				continue;

			// Add to the average and increment the number of targets in the average.
			average += GameManager.m_Players [i].m_Instance.transform.position;
			numTargets++;
		}

		// If there are targets divide the sum of the positions by the number of them to find the average.
		if (numTargets > 0)
			average /= numTargets;

		// Keep the same y value.
		average.z = transform.position.z;

		return average;
	}


	private void Zoom(Vector3 desiredPosition) {
		// Find the required size based on the desired position
		float targetSize = FindRequiredSize (desiredPosition);
		
		foreach(Camera c in m_Camera)
			c.orthographicSize = Mathf.SmoothDamp (c.orthographicSize, targetSize, ref m_ZoomSpeed, m_DampTime);
	}


	private float FindRequiredSize(Vector3 desiredPosition) {
		// Find how far from the rig to the furthest target.
		float targetDistance = MaxTargetDistance (desiredPosition);

		// Calculate the size based on the previously found ratio and buffer.
		float newSize = targetDistance * m_ConvertDistanceToSize * (targetDistance * m_ScreenEdgeBuffer);

		// Restrict the new size so that it's not smaller than the minimum size.
		newSize = Mathf.Min (newSize, m_MaxSize);
		return newSize;
	}


	private float MaxTargetDistance(Vector3 desiredPosition) {
		// Default furthest distance is no distance at all.
		float furthestDistance = 0f;

		// Go through all the targets and if they are further away use that distance instead.
		for (int i = 0; i < GameManager.m_Players.Count; i++) {
			// If the target isn't active, on to the next one.
			if (!GameManager.m_Players [i].m_Instance.activeSelf)
				continue;

			// Find the distance from the camera's desired position to the target.
			float targetDistance = (desiredPosition - GameManager.m_Players [i].m_Instance.transform.position).magnitude;

			// If it's greater than the current furthest distance, it's the furthest distance.
			if (targetDistance > furthestDistance) {
				furthestDistance = targetDistance;
			}
		}

		// Return the distance to the target that is furthest away.
		return furthestDistance;
	}


	public void SetAppropriatePositionAndSize() {
		// Set orthographic size and position without damping.
		transform.position = FindAveragePosition ();
		foreach(Camera c in m_Camera)
			c.orthographicSize = FindRequiredSize (transform.position);
	}
}