using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerSubWeapons : NetworkBehaviour {
	public float fireRate = 1f;
	public GameObject m_FireBallPrefab;
	public PlayerManager m_Manager;
	public Rigidbody _playerRigidbody;
	private PlayerHealth _playerHealth;
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

		if (Input.GetMouseButton (1)) {
			if (timer <= 0) {
				CmdFireBall ();
				timer = fireRate;
			}
			
		}
	}

	[Command]
	public void CmdFireBall() {
		if (!isClient) //avoid to create bullet twice (here & in Rpc call) on hosting client
			CreateFireBall ();

		RpcFireBall ();
	}

	[ClientRpc]
	public void RpcFireBall() {
		CreateFireBall ();
	}

	public void CreateFireBall() {
		Vector3[] vectorBase = {
			_playerRigidbody.rotation * Vector3.right,
			_playerRigidbody.rotation * Vector3.up,
			_playerRigidbody.rotation * Vector3.forward
		};
		Vector3 offsets = vectorBase [2];

		GameObject bullet = Instantiate (m_FireBallPrefab, _playerRigidbody.position + offsets, Quaternion.identity) as GameObject;
		FireBall fireballScript = bullet.GetComponent<FireBall> ();

		fireballScript.originalDirection = vectorBase [2];

		fireballScript.owner = this;
		fireballScript.manager = m_Manager;
	}
}
