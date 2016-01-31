using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerController : NetworkBehaviour {
	public float m_Speed = 7f;
	public float m_RotSpeed = 7f;
	public int m_PlayerNumber = 1;
	public int m_LocalID = 1;
	public PlayerManager m_Manager; 
	private Vector3 _movement = Vector3.zero;
	public Rigidbody _playerRigidbody;
	private PlayerHealth _playerHealth;

	// Use this for initialization
	private void Awake() {
		_playerRigidbody = GetComponent<Rigidbody>();
	}

	public override void OnStartLocalPlayer () {
		base.OnStartLocalPlayer ();
		_playerHealth = GetComponent<PlayerHealth> ();
	}

	// Update is called once per frame
	void FixedUpdate(){
		if (!isLocalPlayer || !_playerHealth.m_IsAlive)
			return;
		
		Move ();
		Turning ();
	}

	void Move(){
		_movement.Set(Input.GetAxisRaw("Horizontal1"), 0f, Input.GetAxisRaw("Vertical1"));
		_movement = _movement.normalized * m_Speed;
		if (_movement != Vector3.zero)
			_playerRigidbody.MovePosition (_playerRigidbody.position + _movement * Time.fixedDeltaTime);
		else
			_playerRigidbody.velocity = Vector3.zero;
	}

	void Turning(){
		Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit floorHit;
		if(Physics.Raycast(camRay, out floorHit, 100f)){
			Vector3 playerToMouse = floorHit.point - transform.position;
			playerToMouse.y = 0f;

			Quaternion newRotation = Quaternion.LookRotation(playerToMouse);
			_playerRigidbody.rotation = Quaternion.Lerp (_playerRigidbody.rotation, newRotation, m_RotSpeed * Time.fixedDeltaTime);
		}
	}

	public void SetDefaults() {
		_playerRigidbody.velocity = Vector3.zero;
		_playerRigidbody.angularVelocity = Vector3.zero;
	}
}