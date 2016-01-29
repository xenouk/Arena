using UnityEngine;
using System.Collections;

public class FireBall : MonoBehaviour {
	public int damage = 100;
	public float speed = 100f;
	public Vector3 originalDirection;
	public PlayerWeapons owner;
	public PlayerManager manager;
	public GameObject m_Explosion;

	private void Start() {
		Destroy(gameObject, 5.0f);
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

	//called on client when the Network destroy that object (it was destroyed on server)
	public void OnDestroy() {

		m_Explosion.SetActive (true);
		m_Explosion.transform.parent = null;
		//set the particle to be destroyed at the end of their lifetime
		Destroy (m_Explosion, 2f);
	}
}
