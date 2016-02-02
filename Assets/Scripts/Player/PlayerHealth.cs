using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class PlayerHealth : NetworkBehaviour {
	public int m_BaseHealth = 100;
	public int m_BaseShield = 100;
	public Slider m_ShieldSlider;
	public Slider m_HealthSlider;                     // The slider to represent how much health the tank currently has.
	public Image m_FillImage;                         // The image component of the slider.
	public Color m_FullHealthColor = Color.green;     // The color the health bar will be when on full health.
	public Color m_ZeroHealthColor = Color.red;
	public GameObject m_HealthCanvas;
	public GameObject m_ShileCanvas;
	public GameObject m_NameCanvas;
	public GameObject m_Model;
	public GameObject m_Shield;
	public BoxCollider m_Collider; 
	public PlayerManager m_Manager; 
	public PlayerWeapons m_Weapons;

	[SyncVar(hook = "OnCurrentHealthChanged")]
	private int m_CurrentHealth;
	[SyncVar(hook = "OnCurrentShieldChanged")]
	private int m_CurrentShield;
	[SyncVar]
	public bool m_IsAlive;

	void Awake (){
		m_IsAlive = true;
		m_HealthSlider.value = m_CurrentHealth = m_BaseHealth;
		m_HealthSlider.maxValue = m_BaseHealth;
		m_Collider = GetComponent<BoxCollider> ();
		m_Weapons = GetComponent<PlayerWeapons> ();
	}

	public void TakeDamage(int amount, PlayerManager from) {
		if (!isServer || !m_IsAlive)
			return;

		if (m_CurrentShield > 0) {
			m_CurrentShield -= amount;
			amount = (m_CurrentShield > 0) ? 0 : Mathf.Abs (m_CurrentShield);
		}

		m_CurrentHealth -= amount;

		if(m_CurrentHealth <= 0){
			from.GetKills++;
			GameManager.s_Instance.RpcUpdateStatus ();
			m_CurrentHealth = m_BaseHealth;
			RpcSetPlayerctive (false);
			switch (GameManager.m_MatchMode) {
			case 0:
				StartCoroutine(SetRespawn());
				break;
			case 1: 
				break;
			}
		}
	}

	public void Heal(int amount) {
		if (!isServer || !m_IsAlive)
			return;

		m_CurrentHealth += amount;

		if(m_CurrentHealth > m_BaseHealth){
			m_CurrentHealth = m_BaseHealth;
		}
	}

	public void AddShield() {
		if (!isServer || !m_IsAlive)
			return;
		
		m_CurrentShield = m_BaseShield;
	}

	private void SetHealthUI(){
		m_HealthSlider.value = m_CurrentHealth;
		m_ShieldSlider.value = m_CurrentShield;
		m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_HealthSlider.value / m_HealthSlider.maxValue);
	}

	void OnCurrentHealthChanged(int value) {
		m_CurrentHealth = value;

		SetHealthUI ();
	}

	void OnCurrentShieldChanged(int value) {
		m_CurrentShield = value;
		m_ShileCanvas.SetActive (m_CurrentShield > 0);
		m_Shield.SetActive (m_CurrentShield > 0);
		SetHealthUI ();
	}

	private void SetPlayerctive(bool active) {
		m_Collider.enabled = active;
		m_IsAlive = active;
		m_Model.SetActive (active);
		m_HealthCanvas.SetActive (active);
		m_NameCanvas.SetActive (active);
		m_Weapons.enabled = active;
		m_Weapons.SetDefaults ();
		if (active) m_Manager.EnableControl();
		else m_Manager.DisableControl();
	}

	public void SetDefaults() {
		m_CurrentHealth = m_BaseHealth;
		m_ShileCanvas.SetActive (false);
		m_Shield.SetActive (false);
		m_CurrentShield = 0;
		SetPlayerctive (true);
	}

	IEnumerator SetRespawn(){
		RpcSetPlayerctive (false);
		yield return new WaitForSeconds (1);
		RpcRespawn ();
		yield return new WaitForSeconds (0.2f);
		RpcSetPlayerctive (true);
	}

	[ClientRpc]
	void RpcSetPlayerctive(bool active){
		SetPlayerctive (active);
	}

	[ClientRpc]
	void RpcRespawn(){
		transform.position = GameManager.s_Instance.m_SpawnPoints.transform.GetChild(Random.Range(0, 
			GameManager.s_Instance.m_SpawnPoints.transform.childCount)).position;
	}
}