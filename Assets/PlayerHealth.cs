using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class PlayerHealth : NetworkBehaviour {
	public int m_BaseHealth = 100;
	public Slider m_Slider;                           // The slider to represent how much health the tank currently has.
	public Image m_FillImage;                         // The image component of the slider.
	public Color m_FullHealthColor = Color.green;     // The color the health bar will be when on full health.
	public Color m_ZeroHealthColor = Color.red;
	public GameObject m_HealthCanvas;
	public GameObject m_Model;
	public BoxCollider m_Collider; 
	public PlayerManager m_Manager; 

	[SyncVar(hook = "OnCurrentHealthChanged")]
	private int m_CurrentHealth;
	[SyncVar]
	public bool m_IsAlive;

	void Awake (){
		m_IsAlive = true;
		m_Slider.value = m_CurrentHealth = m_BaseHealth;
		m_Slider.maxValue = m_BaseHealth;
		m_Collider = GetComponent<BoxCollider> ();
	}

	public void TakeDamage(int amount) {
		if (!isServer || !m_IsAlive)
			return;

		m_CurrentHealth -= amount;

		if(m_CurrentHealth <= 0){
			m_CurrentHealth = m_BaseHealth;
			SetPlayerctive (false);
			RpcRespawn ();
		}
	}

	private void SetHealthUI(){
		m_Slider.value = m_CurrentHealth;

		m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_Slider.value / m_Slider.maxValue);
	}

	void OnCurrentHealthChanged(int value) {
		m_CurrentHealth = value;

		SetHealthUI ();
	}

	private void SetPlayerctive(bool active) {
		m_Collider.enabled = active;
		m_IsAlive = active;
		m_Model.SetActive (active);
		m_HealthCanvas.SetActive (active);

		if (active) m_Manager.EnableControl();
		else m_Manager.DisableControl();
	}

	IEnumerator SetRespawn(){
		yield return new WaitForSeconds (1);
		SetPlayerctive (true);
	}

	[ClientRpc]
	void RpcRespawn(){
		SetPlayerctive (false);
		transform.position = new Vector3 (Random.Range (14, -14), 0.5f, Random.Range (9, -9));
		StartCoroutine (SetRespawn ());
	}

	public void SetDefaults() {
		m_CurrentHealth = m_BaseHealth;
		SetPlayerctive (true);
	}
}