using UnityEngine;
using System.Collections;

public class Mine : MonoBehaviour {
	public SpriteRenderer m_Icon;
	public Sprite[] m_RandomIcon; 
	public int damage = 150;
	public float m_DamageRadius = 3f;
	public float m_Duration = 15f;
	public PlayerWeapons owner;
	public PlayerManager m_Manager;
	public GameObject m_Explosion;

	private void Start() {
		Destroy (gameObject, m_Duration);
		m_Icon.sprite = m_RandomIcon [Random.Range(0, m_RandomIcon.Length)];
	}

	void OnTriggerEnter (Collider other){
		if (other.gameObject == owner.gameObject)
			return;

		Collider[] colliders = Physics.OverlapSphere (other.transform.position, m_DamageRadius, LayerMask.GetMask("Player"));

		foreach (Collider collider in colliders) {
			var playerHealth = collider.GetComponent<PlayerHealth> ();
			if (playerHealth != null && collider.gameObject != owner.gameObject) {
				playerHealth.TakeDamage (damage, m_Manager);
			}
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
