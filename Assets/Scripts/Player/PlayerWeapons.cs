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
	bool _left;
	bool _mainWeapon = false;
	bool _subWeapon = false;
	delegate IEnumerator SlotDelegate();
	SlotDelegate slotDelegate;

	[Header("Laser")]
	public Sprite m_LaserIcon;
	public LineRenderer m_LaserLine;
	public float m_LaserDamageRate;
	public float m_LaserDamageRadius;
	public int m_LaserDamage;
	public int m_LaserBaseAmount = 10;
	[SyncVar(hook = "OnLaserAmountChange")] 
	public float m_LaserAmount;

	[Header("Fire Ball")]
	public Sprite m_FireBallIcon;
	public float m_FireBallRate = 1f;
	public GameObject m_FireBallPrefab;
	public int m_FireBallBaseAmount = 10;
	[SyncVar(hook = "OnFireBallAmountChange")] 
	public int m_FireBallAmount;

	[Header("Splash Bullet")]
	public Sprite m_BulletIcon;
	public GameObject m_bulletPrefab;
	public float m_BulletFireRate = 0.2f;
	public int m_BulletBaseAmount = 15;
	[SyncVar(hook = "OnBulletAmountChange")] 
	public int m_BulletAmount;

	[Header("Tower")]
	public Sprite m_TowerIcon;
	public GameObject m_TowerPrefab;
	[SyncVar(hook = "OnTowerAmountChange")] 
	public bool m_hasTower;

	[Header("Mine")]
	public Sprite m_MineIcon;
	public GameObject m_MinePrefab;
	[SyncVar(hook = "OnMineAmountChange")] 
	public bool m_hasMine;

	[Header("GUI")]
	[SyncVar]public bool _SlotEmpty = true;
	private Text _WeaponAmounts;
	private Image _WeaponIcons;

	// Use this for initialization
	private void Awake() {
		_playerRigidbody = GetComponent<Rigidbody>();
		_WeaponAmounts = GameObject.FindGameObjectWithTag ("WeaponAmount").GetComponent<Text> ();
		_WeaponIcons = GameObject.FindGameObjectWithTag ("WeaponIcon").GetComponent<Image> ();
	}

	public override void OnStartLocalPlayer () {
		base.OnStartLocalPlayer ();
		_playerHealth = GetComponent<PlayerHealth> ();

	}

	public void SetDefaults(){
		slotDelegate = null;
		_SlotEmpty = true;
		_WeaponAmounts.text = "";
		_WeaponIcons.enabled = false;
		_mainWeapon = false;
		_subWeapon = false;
		m_LaserLine.enabled = false;
		m_hasTower = false;
		m_hasMine = false;
		m_BulletAmount = 0;
		m_LaserAmount = 0;
		m_FireBallAmount = 0;
	}

	private void OnDisable(){
		m_LaserLine.enabled = false;
	}

	[ClientCallback]
	void Update() {
		if (!isLocalPlayer || !_playerHealth.m_IsAlive)
			return;

		if (Input.GetMouseButtonDown (0)  && !_mainWeapon) {
			StartCoroutine (ShootBullet ());
		}

		if (Input.GetMouseButtonDown (1) && !_subWeapon) {
			if(slotDelegate != null)
				StartCoroutine(slotDelegate ());
		}

		if (Input.GetMouseButtonUp (1))
			CmdStopLaser ();

		if (_SlotEmpty) {
			_WeaponAmounts.text = "";
			_WeaponIcons.enabled = false;
		} else {
			_WeaponIcons.enabled = true;
		}
	}

	IEnumerator ShootBullet(){
		while (Input.GetMouseButton (0)) {
			_mainWeapon = true;
			CmdFireBullet ();
			yield return new WaitForSeconds(m_BulletFireRate);
			_mainWeapon = false;
		}
	}

	IEnumerator ShootFireBall(){
		while (Input.GetMouseButton (1)) {
			_subWeapon = true;
			CmdFireBall ();
			yield return new WaitForSeconds(m_FireBallRate);
			_subWeapon = false;
		}
	}

	IEnumerator ShootLaser(){
		while (Input.GetMouseButton (1)) {
			_subWeapon = true;
			if (m_LaserAmount > 0)
				CmdFireLaser ();
			yield return null;
			_subWeapon = false;
		}
	}

	IEnumerator ShootSplashBullet(){
		while (Input.GetMouseButton (1)) {
			_subWeapon = true;
			if (m_BulletAmount > 0) {
				CmdFireSplashBullet ();
			}
			yield return new WaitForSeconds(m_BulletFireRate);
			_subWeapon = false;
		}
	}

	IEnumerator PutTower(){
		while (Input.GetMouseButton (1)) {
			_subWeapon = true;
			if (m_hasTower) {
				CmdPutTower ();
			}
			yield return new WaitForSeconds (1f);
			_subWeapon = false;
		}
	}

	IEnumerator PutMine(){
		while (Input.GetMouseButton (1)) {
			_subWeapon = true;
			if (m_hasMine) {
				CmdPutMine ();
			}
			yield return new WaitForSeconds (1f);
			_subWeapon = false;
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

		Vector3 newOffset = (_left) ? offsets [0] : offsets [1];
		_left = !_left;

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
		if (m_FireBallAmount <= 0) {
			if (_SlotEmpty) {
				_SlotEmpty = false;
				slotDelegate = ShootFireBall;
			}
		}

		m_FireBallAmount = amount;

		if (amount > 0) {
			if (isLocalPlayer) {
				_WeaponIcons.sprite = m_FireBallIcon;
				_WeaponAmounts.text = Mathf.FloorToInt(m_FireBallAmount).ToString ();
			}
		} else {
			_SlotEmpty = true;
			slotDelegate = null;
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
		if (m_FireBallAmount < 0)
			return;

		m_FireBallAmount--;

		Vector3 offsets = _playerRigidbody.rotation * Vector3.forward;

		GameObject bullet = Instantiate (m_FireBallPrefab, _playerRigidbody.position + offsets, Quaternion.identity) as GameObject;
		FireBall fireballScript = bullet.GetComponent<FireBall> ();

		fireballScript.originalDirection = offsets;

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
			if (_SlotEmpty) {
				_SlotEmpty = false;
				slotDelegate = ShootLaser;
			}
		}

		m_LaserAmount = amount;

		if (amount > 0) {
			if (isLocalPlayer) {
				_WeaponIcons.sprite = m_LaserIcon;
				_WeaponAmounts.text = Mathf.FloorToInt(m_LaserAmount).ToString ();
			}
		} else {
			if (isLocalPlayer) {
				_WeaponIcons.sprite = null;
				_WeaponAmounts.text = "";
			}

			_SlotEmpty = true;
			slotDelegate = null;
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
		if (m_LaserAmount < 0)
			return;

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
		if (m_BulletAmount <= 0) {
			if (_SlotEmpty) {
				_SlotEmpty = false;
				slotDelegate = ShootSplashBullet;
			}
		}

		m_BulletAmount = amount;

		if (amount > 0) {
			if (isLocalPlayer) {
				_WeaponIcons.sprite = m_BulletIcon;
				_WeaponAmounts.text = Mathf.FloorToInt(m_BulletAmount).ToString ();
			}
		} else {
			_SlotEmpty = true;
			slotDelegate = null;
		}
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
		if (m_BulletAmount < 0)
			return;
		
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

	#region Put Tower
	public void AddTowerAmount(){
		if (!isServer)
			return;

		m_hasTower = true;
	}

	void OnTowerAmountChange(bool active){

		m_hasTower = active;

		if (m_hasTower) {
			if (_SlotEmpty) {
				_SlotEmpty = false;
				slotDelegate = PutTower;
			}

			if (isLocalPlayer) {
				_WeaponIcons.sprite = m_TowerIcon;
			}
		} else {
			_SlotEmpty = true;
			slotDelegate = null;
		}
	}

	[Command]
	public void CmdPutTower() {
		if (!isClient) //avoid to create bullet twice (here & in Rpc call) on hosting client
			CreateTower ();

		RpcPutTower ();
	}

	[ClientRpc]
	public void RpcPutTower() {
		CreateTower ();
	}

	public void CreateTower() {
		if (!m_hasTower)
			return;
		
		m_hasTower = false;


		GameObject bullet = Instantiate (m_TowerPrefab, _playerRigidbody.position, Quaternion.identity) as GameObject;
		Tower towerScript = bullet.GetComponent<Tower> ();

		towerScript.owner = this;
		towerScript.m_Manager = m_Manager;
	}
	#endregion

	#region Put Mine
	public void AddMineAmount(){
		if (!isServer)
			return;

		m_hasMine = true;
	}

	void OnMineAmountChange(bool active){

		m_hasMine = active;

		if (m_hasMine) {
			if (_SlotEmpty) {
				_SlotEmpty = false;
				slotDelegate = PutMine;
			}

			if (isLocalPlayer) {
				_WeaponIcons.sprite = m_MineIcon;
			}
		} else {
			_SlotEmpty = true;
			slotDelegate = null;
		}
	}

	[Command]
	public void CmdPutMine() {
		if (!isClient) //avoid to create bullet twice (here & in Rpc call) on hosting client
			CreateMine ();

		RpcPutMine ();
	}

	[ClientRpc]
	public void RpcPutMine() {
		CreateMine ();
	}

	public void CreateMine() {
		if (!m_hasMine)
			return;
		
		m_hasMine = false;

		GameObject mine = Instantiate (m_MinePrefab, _playerRigidbody.position, Quaternion.identity) as GameObject;
		Mine mineScript = mine.GetComponent<Mine> ();

		mineScript.owner = this;
		mineScript.m_Manager = m_Manager;
	}
	#endregion
}