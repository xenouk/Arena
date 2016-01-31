using UnityEngine;
using System.Collections;

public class Tower : MonoBehaviour {
	public float m_FireRate = 0.2f;
	public float m_rotSpeed = 50F;
	public float m_destroyTime = 10f;
	public GameObject m_BulletPrefap;
	public PlayerWeapons owner;
	public PlayerManager m_Manager;
	float timer;
	Rigidbody _body;

	void Start() {
		Destroy(gameObject, m_destroyTime);
		_body = GetComponent<Rigidbody> ();
	}

	void Update () {
		transform.Rotate (Vector3.up * Time.deltaTime * m_rotSpeed);
		timer -= Time.deltaTime;

		if (timer <= 0) {
			Vector3[] offsets = {
				_body.rotation * Vector3.forward,
				_body.rotation * Vector3.left,
				_body.rotation * Vector3.back,
				_body.rotation * Vector3.right
			};

			Vector3[] originalDirections = {
				Quaternion.Euler(1, 1, 1) * _body.rotation * Vector3.forward,
				Quaternion.Euler(1, 90, 1) * _body.rotation * Vector3.forward,
				Quaternion.Euler(1, 180, 1) * _body.rotation * Vector3.forward,
				Quaternion.Euler(1, 270, 1) * _body.rotation * Vector3.forward
			};

			for (int i = 0; i < offsets.Length; i++) {
				GameObject bullet = Instantiate (m_BulletPrefap, _body.position + offsets[i], Quaternion.identity) as GameObject;
				Bullet bulletScript = bullet.GetComponent<Bullet> ();

				bulletScript.originalDirection = originalDirections [i];

				bulletScript.owner = owner;
				bulletScript.manager = m_Manager;
			}

			timer = m_FireRate;
		}
	}
}
