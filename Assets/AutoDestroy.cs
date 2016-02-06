using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class AutoDestroy : NetworkBehaviour {
	public float time = 2f;
	// Use this for initialization
	void Start () {
		Invoke ("DestroyObject", time);
	}

	[ServerCallback]
	void DestroyObject(){
		NetworkServer.Destroy(gameObject);
	}
}
