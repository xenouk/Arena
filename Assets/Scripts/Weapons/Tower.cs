using UnityEngine;
using System.Collections;

public class Tower : MonoBehaviour {
	public float m_FireRate = 0.2f;
	public float m_rotSpeed = 50F;
	public float m_destroyTime = 10f;
	public GameObject m_BulletPrefap;
	public PlayerWeapons owner;
	public PlayerManager m_Manager;
	public AudioSource a_Audio;
	float timer;

	void Start() {
		Destroy(gameObject, m_destroyTime);
	}

	void Update () {
		transform.Rotate (Vector3.forward * Time.deltaTime * m_rotSpeed);
		timer -= Time.deltaTime;

		if (timer <= 0) {
			a_Audio.Play ();
			Quaternion[] originalDirections = {
				transform.rotation,
				Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0,0,90)),
				Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0,0,180)),
				Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0,0,270))
			};

			for (int i = 0; i < originalDirections.Length; i++) {
				GameObject bullet = Instantiate (m_BulletPrefap, transform.position, originalDirections [i]) as GameObject;
				Bullet bulletScript = bullet.GetComponent<Bullet> ();

				bulletScript.owner = owner;
				bulletScript.manager = m_Manager;
			}

			timer = m_FireRate;
		}
	}
}
