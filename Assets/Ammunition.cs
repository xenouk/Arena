using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Ammunition : NetworkBehaviour {
	public string m_WeaponName;
	// Use this for initialization
	void Start () {
		Invoke ("AutoDestroy", 5f);
	}

	[ServerCallback]
	void OnTriggerEnter (Collider other){
		var playerWeapon = other.GetComponent<PlayerWeapons> ();

		if (playerWeapon != null) {
			if (playerWeapon._SlotAEmpty || playerWeapon._SlotBEmpty) {
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
				}
				NetworkServer.Destroy(gameObject);
			}
		}
	}

	[ServerCallback]
	void AutoDestroy(){
		NetworkServer.Destroy(gameObject);
	}
}
