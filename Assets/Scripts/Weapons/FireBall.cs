using UnityEngine;
using System.Collections;

public class FireBall : MonoBehaviour {
	public int damage = 100;
	public float speed = 100f;
	public float m_DamageRadius = 3;
	public PlayerWeapons owner;
	public PlayerManager manager;
	public GameObject m_Explosion;

	private void Start() {
		Destroy(gameObject, 5.0f);
		GetComponent<Rigidbody2D>().velocity = transform.up * speed;
	}

	void OnTriggerEnter2D (Collider2D other){
		if (other.gameObject == owner.gameObject)
			return;

		Collider2D[] colliders = Physics2D.OverlapCircleAll (other.transform.position, m_DamageRadius, LayerMask.GetMask("Player"));

		foreach (Collider2D collider in colliders) {
			var playerHealth = collider.GetComponent<PlayerHealth> ();
			if (playerHealth != null && collider.gameObject != owner.gameObject) {
				playerHealth.TakeDamage (damage, manager);
			}
		}

		Destroy (gameObject);
	}

	//called on client when the Network destroy that object (it was destroyed on server)
	public void OnDestroy() {
		m_Explosion.SetActive (true);
		m_Explosion.transform.parent = null;
		m_Explosion.GetComponent<AudioSource> ().enabled = true;
		Destroy (m_Explosion, 2f);
	}
}
