﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Bullet : MonoBehaviour {
	public int damage = 20;
	public float speed = 100f;
	public float m_destroyTime = 1f;
	public Vector2 originalDirection;
	public PlayerWeapons owner;
	public PlayerManager manager;

	private void Start() {
		Destroy(gameObject, m_destroyTime);
		GetComponent<Rigidbody2D>().velocity = transform.up * speed;
	}

	void OnTriggerEnter2D (Collider2D other){
		if (other.gameObject == owner.gameObject)
			return;

		var playerHealth = other.GetComponent<PlayerHealth> ();
		if (playerHealth != null) {
			playerHealth.TakeDamage (damage, manager);
		}

		Destroy (gameObject);
	}
}
