using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Mine : NetworkBehaviour {
	public SpriteRenderer m_Icon;
	public Sprite[] m_RandomIcon; 
	public int damage = 150;
	public float m_DamageRadius = 3f;
	public float m_Duration = 15f;
	public PlayerWeapons owner;
	public PlayerManager m_Manager;
	public GameObject m_Particle;

	private void Start() {
		Invoke ("Destroy", m_Duration);
		m_Icon.sprite = m_RandomIcon [Random.Range(0, m_RandomIcon.Length)];
	}

	[ServerCallback]
	void OnTriggerEnter2D (Collider2D other){
		if (other.gameObject == owner.gameObject)
			return;

		Collider2D[] colliders = Physics2D.OverlapCircleAll (other.transform.position, m_DamageRadius, LayerMask.GetMask("Player"));

		foreach (Collider2D collider in colliders) {
			var playerHealth = collider.GetComponent<PlayerHealth> ();
			if (playerHealth != null && collider.gameObject != owner.gameObject) {
				playerHealth.TakeDamage (damage, m_Manager);
			}
		}

		if (isServer)
			NetworkServer.Spawn (Instantiate (m_Particle, transform.position, Quaternion.identity) as GameObject);

		NetworkServer.Destroy (gameObject);
	}

	[ServerCallback]
	void Destroy(){
		NetworkServer.Destroy(gameObject);
	}
}
