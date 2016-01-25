using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerController : NetworkBehaviour {
	public float m_Speed = 7f;
	public float m_RotSpeed = 7f;
	public float fireRate = 0.2f;
	public float m_bulletSpeed = 50f;
	public int m_PlayerNumber = 1;
	public int m_LocalID = 1;
	private Vector3 _movement = Vector3.zero;
	public Rigidbody _playerRigidbody;
	private PlayerHealth _playerHealth;
	[SerializeField] GameObject _bulletPrefab;
	float timer;

	// Use this for initialization
	private void Awake() {
		_playerRigidbody = GetComponent<Rigidbody>();
	}

	public override void OnStartLocalPlayer () {
		base.OnStartLocalPlayer ();
		_playerHealth = GetComponent<PlayerHealth> ();
	}

	[ClientCallback]
	void Update() {
		if (!isLocalPlayer || !_playerHealth.m_IsAlive)
			return;

		timer -= Time.fixedDeltaTime;

		if (Input.GetMouseButton (0) && timer <= 0) {
			CmdFire(transform.position, transform.forward, _playerRigidbody.velocity);
			timer = fireRate;
		}

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

	[Command]
	public void CmdFire(Vector3 position, Vector3 forward, Vector3 startingVelocity) {
		if (!isClient) //avoid to create bullet twice (here & in Rpc call) on hosting client
			CreateBullets ();

		RpcFire ();
	}

	[ClientRpc]
	public void RpcFire() {
		CreateBullets ();
	}

	public void CreateBullets() {
		Vector3[] vectorBase = {
			_playerRigidbody.rotation * Vector3.right,
			_playerRigidbody.rotation * Vector3.up,
			_playerRigidbody.rotation * Vector3.forward
		};
		Vector3[] offsets = {
			-.5f * vectorBase [0] + -0.4f * vectorBase [2],
			.5f * vectorBase [0] + -0.4f * vectorBase [2]
		};

		for (int i = 0; i < 2; ++i) {
			GameObject bullet = Instantiate (_bulletPrefab, _playerRigidbody.position + offsets [i], Quaternion.identity) as GameObject;
			Bullet bulletScript = bullet.GetComponent<Bullet> ();

			bulletScript.originalDirection = vectorBase [2];

			bulletScript.owner = this;
		}
	}

	public void SetDefaults() {
		_playerRigidbody.velocity = Vector3.zero;
		_playerRigidbody.angularVelocity = Vector3.zero;
	}
}
