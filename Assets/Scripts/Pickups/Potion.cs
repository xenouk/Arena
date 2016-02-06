using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Potion : NetworkBehaviour {
	public int healAmount = 50;
	public GameObject m_Particle;
	// Use this for initialization
	void Start () {
		Invoke ("AutoDestroy", 4f);
	}

	[ServerCallback]
	void OnTriggerEnter2D (Collider2D other){
		var playerHealth = other.GetComponent<PlayerHealth> ();
		if (playerHealth != null) {
			playerHealth.Heal (healAmount);
			if (isServer)
				NetworkServer.Spawn (Instantiate (m_Particle, transform.position, Quaternion.identity) as GameObject);
			NetworkServer.Destroy(gameObject);
		}
	}

	[ServerCallback]
	void AutoDestroy(){
		NetworkServer.Destroy(gameObject);
	}
}