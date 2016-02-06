using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerWeapons : NetworkBehaviour {
	public PlayerManager m_Manager;
	public Rigidbody2D _playerRigidbody;
	private PlayerHealth _playerHealth;
	public PlayerController _playerController;
	bool _left;
	bool _mainWeapon = false;
	bool _subWeapon = false;
	delegate IEnumerator SlotDelegate();
	SlotDelegate slotDelegate;

	[Header("Bullet")]
	public GameObject m_bulletPrefab;
	public float m_BulletFireRate = 0.2f;

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
	public GameObject m_SplashBulletPrefab;
	public float m_SplashBulletFireRate = 0.2f;
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

	[Header("Audio")]
	public AudioSource a_Audio;
	public AudioClip a_Shooting;
	public AudioClip a_Laser;

	// Use this for initialization
	private void Awake() {
		_playerRigidbody = GetComponent<Rigidbody2D>();
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
		a_Audio.Stop ();
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
			yield return new WaitForSeconds(m_SplashBulletFireRate);
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
		a_Audio.clip = a_Shooting;
		a_Audio.Play ();

		Vector3[] offsets = {
			transform.position + transform.right * .5f,
			transform.position + transform.right * -.5f
		};
		
		_left = !_left;
		Vector3 newOffset = (_left) ? offsets [0] : offsets [1];

		GameObject bullet = Instantiate (m_bulletPrefab, newOffset, transform.rotation) as GameObject;
		Bullet bulletScript = bullet.GetComponent<Bullet> ();

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

		Vector2 offsets = transform.position + transform.up * 0.2f;

		GameObject bullet = Instantiate (m_FireBallPrefab, offsets, transform.rotation) as GameObject;
		FireBall fireballScript = bullet.GetComponent<FireBall> ();

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
		if (a_Audio.clip == a_Laser)
			a_Audio.Stop ();
	}

	void FireLaser(){
		if (m_LaserAmount < 0)
			return;

		a_Audio.clip = a_Laser;
		if(!a_Audio.isPlaying)
			a_Audio.Play ();

		m_LaserLine.enabled = true;
		m_LaserAmount -= Time.deltaTime * m_LaserDamageRate;
		Vector3 origin = transform.position + transform.rotation * Vector3.up * 1.2f;
		Vector3 dir = transform.rotation * Vector3.up;
		RaycastHit2D hit = Physics2D.Raycast (origin, dir, Mathf.Infinity, LayerMask.GetMask ("Player", "Wall"));
		m_LaserLine.SetPosition (0, origin);
		if (hit.collider != null) {
			m_LaserLine.SetPosition (1, hit.point);
			RaycastHit2D[] hits = Physics2D.CircleCastAll (origin, m_LaserDamageRadius, dir, Vector3.Distance (_playerRigidbody.position, hit.point), LayerMask.GetMask ("Player"));
			foreach (RaycastHit2D h in hits) {
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
		a_Audio.clip = a_Shooting;
		m_BulletAmount--;

		Vector3[] offsets = {
			transform.position + transform.right,
			transform.position + transform.right * .5f,
			transform.position,
			transform.position + transform.right * -.5f,
			transform.position + transform.right * -1,
		};

		Quaternion[] originalDirections = {
			Quaternion.Euler(1, 1, -6) * transform.rotation,
			Quaternion.Euler(1, 1, -3) * transform.rotation,
			transform.rotation,
			Quaternion.Euler(1, 1, 3) * transform.rotation,
			Quaternion.Euler(1, 1, 6) * transform.rotation,
		};

		for (int i = 0; i < offsets.Length; i++) {
			a_Audio.Play ();
			GameObject bullet = Instantiate (m_SplashBulletPrefab, offsets[i], originalDirections[i]) as GameObject;
			Bullet bulletScript = bullet.GetComponent<Bullet> ();

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
		
		CreateMine ();

		//RpcPutMine ();
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
		NetworkServer.Spawn (mine);
	}
	#endregion
}