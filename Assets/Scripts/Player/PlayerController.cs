using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerController : NetworkBehaviour {
	public float m_Speed = 7f;
	public float m_RotSpeed = 90f;
	public int m_PlayerNumber = 1;
	public int m_LocalID = 1;
	public PlayerManager m_Manager; 
	public Rigidbody2D _playerRigidbody;
	private PlayerHealth _playerHealth;
	public TrailRenderer[] m_Trails;

	// Use this for initialization
	private void Awake() {
		_playerRigidbody = GetComponent<Rigidbody2D>();
	}

	public override void OnStartLocalPlayer () {
		base.OnStartLocalPlayer ();
		_playerHealth = GetComponent<PlayerHealth> ();
	}

	void FixedUpdate(){
		if (!isLocalPlayer || !_playerHealth.m_IsAlive)
			return;

		Move ();
	}

	void Move(){
		_playerRigidbody.velocity = Vector2.zero;
		_playerRigidbody.angularVelocity = 0;

		Quaternion rot = transform.rotation;

		float z = rot.eulerAngles.z;

		z -= Input.GetAxis ("Horizontal1") * m_RotSpeed * Time.fixedDeltaTime;

		rot = Quaternion.Euler (0, 0, z);

		transform.rotation = rot;

		Vector3 pos = new Vector3(_playerRigidbody.position.x, _playerRigidbody.position.y, 0);

		Vector3 velocity = new Vector3(0, Input.GetAxis ("Vertical1") * m_Speed * Time.fixedDeltaTime, 0);

		pos += rot * velocity;

		transform.position = pos;

		if (velocity.y < 0) {
			m_Trails [0].time = 0f;
			m_Trails [1].time = 0f;
		} else {
			m_Trails [0].time = 0.5f;
			m_Trails [1].time = 0.5f;
		}
	}

	public void SetDefaults() {
		_playerRigidbody.velocity = Vector2.zero;
		_playerRigidbody.angularVelocity = 0;
	}
}