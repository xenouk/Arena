using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Potion : NetworkBehaviour {
	public int healAmount = 50;
	// Use this for initialization
	void Start () {
		Invoke ("AutoDestroy", 4f);
	}

	[ServerCallback]
	void OnTriggerEnter (Collider other){
		var playerHealth = other.GetComponent<PlayerHealth> ();
		if (playerHealth != null) {
			playerHealth.Heal (healAmount);
			NetworkServer.Destroy(gameObject);
		}
	}

	[ServerCallback]
	void AutoDestroy(){
		NetworkServer.Destroy(gameObject);
	}
}