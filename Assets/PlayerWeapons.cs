using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerWeapons : NetworkBehaviour {
	public PlayerManager m_Manager;
	public Rigidbody _playerRigidbody;
	private PlayerHealth _playerHealth;
	public PlayerController _playerController;
	float timer;
	bool left;

	[Header("Laser")]
	public Sprite m_LaserIcon;
	public LineRenderer m_LaserLine;
	public float m_LaserDamageRate;
	public float m_LaserDamageRadius;
	public int m_LaserDamage;
	public int m_LaserBaseAmount = 10;
	[SyncVar(hook = "OnLaserAmountChange")] 
	public float m_LaserAmount;
	[SyncVar]public int m_LaserSlot;
	[Header("Fire Ball")]
	public Sprite m_FireBallIcon;
	public float fireRate = 1f;
	public GameObject m_FireBallPrefab;
	public int m_FireBallBaseAmount = 10;
	[SyncVar(hook = "OnFireBallAmountChange")] 
	public int m_FireBallAmount;
	[SyncVar]public int m_FireBallSlot;
	[Header("Bullet")]
	public GameObject m_bulletPrefab;
	public float m_BulletFireRate = 0.2f;
	public int m_BulletBaseAmount = 15;
	[SyncVar(hook = "OnBulletAmountChange")] 
	public int m_BulletAmount;
	[SyncVar]public int m_BulletSlot;
	[Header("Weapon Slots")]
	public Sprite m_BulletIcon;
	private Text[] _WeaponAmounts = new Text[2];
	private Image[] _WeaponIcons = new Image[2];
	[SyncVar]public bool _SlotAEmpty = true;
	[SyncVar]public bool _SlotBEmpty = true;

	delegate void SlotDelegate();
	SlotDelegate slotDelegate;
	SlotDelegate slotADelegate;
	SlotDelegate slotBDelegate;

	// Use this for initialization
	private void Awake() {
		_playerRigidbody = GetComponent<Rigidbody>();
	}

	public override void OnStartLocalPlayer () {
		base.OnStartLocalPlayer ();
		_playerHealth = GetComponent<PlayerHealth> ();
		for (int i = 0; i < _WeaponAmounts.Length; i++) {
			_WeaponAmounts [i] = GameObject.FindGameObjectsWithTag ("WeaponAmount") [i].GetComponent<Text> ();
			_WeaponIcons [i] = GameObject.FindGameObjectsWithTag ("WeaponIcon") [i].GetComponent<Image> ();
		}
	}

	public void SetDefaults(){
		m_LaserLine.enabled = false;
	}

	private void OnDisable(){
		m_LaserLine.enabled = false;
	}



	[ClientCallback]
	void Update() {
		if (!isLocalPlayer || !_playerHealth.m_IsAlive)
			return;

		timer -= Time.deltaTime;

		if (Input.GetKeyDown (KeyCode.LeftShift)) {
			slotDelegate = (slotDelegate == slotADelegate) ? slotBDelegate : slotADelegate;
		}

		if (Input.GetMouseButton (0)) {
			if (timer <= 0) {
				CmdFireBullet ();
				timer = m_BulletFireRate;
			}
		}

		if (Input.GetMouseButton (1)) {

			slotDelegate ();
		}

		if (_SlotAEmpty && _WeaponIcons [0].sprite != null) {
			_WeaponAmounts [0].text = "";
			_WeaponIcons [0].sprite = null;
		}else if (_SlotBEmpty && _WeaponIcons [1].sprite != null) {
			_WeaponAmounts [1].text = "";
			_WeaponIcons [1].sprite = null;
		}

		if (Input.GetMouseButtonUp (1))
			CmdStopLaser ();
	}

	void ShootFireBall(){
		if (timer <= 0) {
			CmdFireBall ();
			timer = fireRate;
		}
	}

	void ShootLaser(){
		if (m_LaserAmount > 0)
			CmdFireLaser ();
	}

	void ShootSplashBullet(){
		if (timer <= 0 && m_BulletAmount > 0) {
			CmdFireSplashBullet ();
			timer = m_BulletFireRate;
		}
	}

	#region Fire Bullet
	[Command]
	public void CmdFireBullet() {
		if (!isClient) //avoid to create bullet twice (here & in Rpc call) on hosting client
			CreateBullets ();

		RpcFireBullet ();
	}

	[ClientRpc]
	public void RpcFireBullet() {
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

		Vector3 newOffset = (left) ? offsets [0] : offsets [1];
		left = !left;

		GameObject bullet = Instantiate (m_bulletPrefab, _playerRigidbody.position + newOffset, Quaternion.identity) as GameObject;
		Bullet bulletScript = bullet.GetComponent<Bullet> ();

		bulletScript.originalDirection = vectorBase [2];

		bulletScript.owner = this;
		bulletScript.manager = m_Manager;
	}
	#endregion

	#region Fire Ball
	public void AddFireBallAmount(){
		if (!isServer)
			return;

		m_FireBallAmount = m_FireBallBaseAmount;
	}

	void OnFireBallAmountChange(int amount){
		CheckAmount (m_FireBallAmount, amount, m_FireBallSlot, m_FireBallIcon, ShootFireBall);
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
	#endregion

	#region Fire Laser
	public void AddLaserAmount(){
		if (!isServer)
			return;

		m_LaserAmount = m_LaserBaseAmount;
	}

	void OnLaserAmountChange(float amount){
		if (m_LaserAmount <= 0) {
			if (_SlotAEmpty) {
				m_LaserSlot = 0;
				_SlotAEmpty = false;
				slotDelegate = slotADelegate = ShootLaser;
			} else if (_SlotBEmpty) {
				m_LaserSlot = 1;
				_SlotBEmpty = false;
				slotDelegate = slotBDelegate = ShootLaser;
			}
		}

		m_LaserAmount = amount;

		if (amount > 0) {
			if (isLocalPlayer) {
				_WeaponIcons [m_LaserSlot].sprite = m_LaserIcon;
				_WeaponAmounts [m_LaserSlot].text = Mathf.FloorToInt(m_LaserAmount).ToString ();
			}
		} else {
			if (isLocalPlayer) {
				_WeaponIcons [m_LaserSlot].sprite = null;
				_WeaponAmounts [m_LaserSlot].text = "";
			}

			if (m_LaserSlot == 0) {
				_SlotAEmpty = true;
			} else if (m_LaserSlot == 1) {
				_SlotBEmpty = true;
			}
			slotDelegate = (!_SlotAEmpty)?slotADelegate:slotBDelegate;
			CmdStopLaser ();
		}
	}

	[Command]
	public void CmdFireLaser() {
		if (!isClient)
			RpcFireLaser ();
		RpcFireLaser ();
	}

	[Command]
	public void CmdStopLaser() {
		if (!isClient)
			StopLaser ();
		RpcStopLaser ();
	}

	[ClientRpc]
	public void RpcFireLaser() {
		FireLaser ();
	}

	[ClientRpc]
	public void RpcStopLaser() {
		StopLaser ();
	}

	void FireLaser(){
		m_LaserLine.enabled = true;
		m_LaserAmount -= Time.deltaTime * m_LaserDamageRate;

		Ray ray = new Ray (_playerRigidbody.position + _playerRigidbody.rotation * Vector3.forward, _playerRigidbody.rotation * Vector3.forward);
		RaycastHit hit;
		m_LaserLine.SetPosition(0, ray.origin);
		if(Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Player", "Wall"))){
			m_LaserLine.SetPosition (1, hit.point);
			RaycastHit[] hits = Physics.SphereCastAll (ray, m_LaserDamageRadius, Vector3.Distance (_playerRigidbody.position, hit.point), LayerMask.GetMask ("Player"));
			foreach (RaycastHit h in hits) {
				if (h.collider.transform != this.transform)
					h.collider.GetComponent<PlayerHealth> ().TakeDamage (m_LaserDamage, m_Manager);
			}
		}
	}

	void StopLaser() {
		m_LaserLine.enabled = false;
	}
	#endregion

	#region Fire Splash Bullet
	public void AddSplashBulletAmount(){
		if (!isServer)
			return;

		m_BulletAmount = m_BulletBaseAmount;
	}

	void OnBulletAmountChange(int amount){
		CheckAmount (m_BulletAmount, amount, m_BulletSlot, m_BulletIcon, ShootSplashBullet);

		/*if (m_BulletAmount <= 0) {
			if (_SlotAEmpty) {
				m_BulletSlot = 0;
				_SlotAEmpty = false;
				slotDelegate = slotADelegate = ShootSplashBullet;
			} else if (_SlotBEmpty) {
				m_BulletSlot = 1;
				_SlotBEmpty = false;
				slotDelegate = slotBDelegate = ShootSplashBullet;
			}
		}

		m_BulletAmount = amount;

		if (amount > 0) {
			if (isLocalPlayer) {
				_WeaponIcons [m_BulletSlot].sprite = m_BulletIcon;
				_WeaponAmounts [m_BulletSlot].text = Mathf.FloorToInt(m_BulletAmount).ToString ();
			}
		} else {
			if (m_BulletSlot == 0) {
				_SlotAEmpty = true;
			} else if (m_BulletSlot == 1) {
				_SlotBEmpty = true;
			}

			slotDelegate = (!_SlotAEmpty)?slotADelegate:slotBDelegate;

			CmdStopLaser ();
		}*/
	}

	[Command]
	public void CmdFireSplashBullet() {
		if (!isClient) //avoid to create bullet twice (here & in Rpc call) on hosting client
			CreateSplashBullet ();

		RpcFireSplashBullet ();
	}

	[ClientRpc]
	public void RpcFireSplashBullet() {
		CreateSplashBullet ();
	}

	public void CreateSplashBullet() {
		m_BulletAmount--;

		Vector3[] vectorBase = {
			_playerRigidbody.rotation * Vector3.right,
			_playerRigidbody.rotation * Vector3.forward
		};

		Vector3[] offsets = {
			-.6f * vectorBase [0] + -0.4f * vectorBase [1],
			-.3f * vectorBase [0] + -0.4f * vectorBase [1],
			-0.4f * vectorBase [1],
			.3f * vectorBase [0] + -0.4f * vectorBase [1],
			.6f * vectorBase [0] + -0.4f * vectorBase [1]
		};

		Vector3[] originalDirections = {
			Quaternion.Euler(1, -6f, 1) * _playerRigidbody.rotation * Vector3.forward,
			Quaternion.Euler(1, -3f, 1) * _playerRigidbody.rotation * Vector3.forward,
			_playerRigidbody.rotation * Vector3.forward,
			Quaternion.Euler(1, 3f, 1) * _playerRigidbody.rotation * Vector3.forward,
			Quaternion.Euler(1, 6f, 1) * _playerRigidbody.rotation * Vector3.forward
		};

		for (int i = 0; i < offsets.Length; i++) {
			GameObject bullet = Instantiate (m_bulletPrefab, _playerRigidbody.position + offsets[i], Quaternion.identity) as GameObject;
			Bullet bulletScript = bullet.GetComponent<Bullet> ();

			bulletScript.originalDirection = originalDirections [i];

			bulletScript.owner = this;
			bulletScript.manager = m_Manager;
		}
	}
	#endregion

	void CheckAmount(int currentAmount, int newAmount, int slot, Sprite icon, SlotDelegate newSlotDelegate){
		if (currentAmount <= 0) {
			if (_SlotAEmpty) {
				slot = 0;
				_SlotAEmpty = false;
				slotDelegate = slotADelegate = newSlotDelegate;
			} else if (_SlotBEmpty) {
				slot = 1;
				_SlotBEmpty = false;
				slotDelegate = slotBDelegate = newSlotDelegate;
			}
		}

		currentAmount = newAmount;

		if (newAmount > 0) {
			if (isLocalPlayer) {
				_WeaponIcons [slot].sprite = icon;
				_WeaponAmounts [slot].text = Mathf.FloorToInt(currentAmount).ToString ();
			}
		} else {
			if (slot == 0) {
				_SlotAEmpty = true;
			} else if (slot == 1) {
				_SlotBEmpty = true;
			}

			slotDelegate = (!_SlotAEmpty)?slotADelegate:slotBDelegate;

			CmdStopLaser ();
		}
	}
}
