using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Ammunition : NetworkBehaviour {
	public string m_WeaponName;
	public GameObject m_Particle;

	void Start () {
		Invoke ("Destroy", 10f);
	}

	[ServerCallback]
	void OnTriggerEnter2D (Collider2D other){
		var playerWeapon = other.GetComponent<PlayerWeapons> ();

		if (playerWeapon != null) {
			if (playerWeapon._SlotEmpty) {
				switch (m_WeaponName) {
				case "Laser":
					playerWeapon.AddLaserAmount ();
					break;
				case "SplashBullet":
					playerWeapon.AddSplashBulletAmount ();
					break;
				case "FireBall":
					playerWeapon.AddFireBallAmount ();
					break;
				case "Tower":
					playerWeapon.AddTowerAmount ();
					break;
				case "Mine":
					playerWeapon.AddMineAmount ();
					break;
				}

				if (isServer)
					NetworkServer.Spawn (Instantiate (m_Particle, transform.position, Quaternion.identity) as GameObject);
				
				NetworkServer.Destroy(gameObject);
			}
		}
	}

	[ServerCallback]
	void Destroy(){
		NetworkServer.Destroy(gameObject);
	}
}
