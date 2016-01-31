using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Shield : NetworkBehaviour {
	// Use this for initialization
	void Start () {
		Invoke ("AutoDestroy", 4f);
	}

	[ServerCallback]
	void OnTriggerEnter (Collider other){
		var playerHealth = other.GetComponent<PlayerHealth> ();
		if (playerHealth != null) {
			playerHealth.AddShield ();
			NetworkServer.Destroy(gameObject);
		}
	}

	[ServerCallback]
	void AutoDestroy(){
		NetworkServer.Destroy(gameObject);
	}
}