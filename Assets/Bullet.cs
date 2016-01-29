using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Bullet : MonoBehaviour {
	public int damage = 20;
	public float speed = 100f;
	public Vector3 originalDirection;
	public PlayerWeapons owner;
	public PlayerManager manager;

	private void Start() {
		Destroy(gameObject, 1.0f);
		GetComponent<Rigidbody>().velocity = originalDirection * speed;
		transform.forward = originalDirection;
	}

	void OnTriggerEnter (Collider other){
		if (other.gameObject == owner.gameObject)
			return;

		var playerHealth = other.GetComponent<PlayerHealth> ();
		if (playerHealth != null) {
			playerHealth.TakeDamage (damage, manager);
		}

		Destroy (gameObject);
	}
}
