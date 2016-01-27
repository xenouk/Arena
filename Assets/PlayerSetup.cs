using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PlayerSetup : NetworkBehaviour {
	[Header("UI")]
	public Text m_NameText;

	[Header("Network")]
	[Space]
	[SyncVar]
	public Color m_Color;

	[SyncVar]
	public string m_PlayerName;

	//this is the player number in all of the players
	[SyncVar]
	public int m_PlayerNumber;

	//This is the local ID when more than 1 player per client
	[SyncVar]
	public int m_LocalID;

	[SyncVar]
	public bool m_IsReady = false;

	[SyncVar]
	public int m_kills;

	//This allow to know if the crown must be displayed or not
	protected bool m_isLeader = false;

	public override void OnStartClient() {
		base.OnStartClient ();

		if (!isServer) //if not hosting, we had the tank to the gamemanger for easy access!
			GameManager.AddPlayer (gameObject, m_PlayerNumber, m_Color, m_PlayerName, m_LocalID);

		GameObject m_PlayerRenderers = transform.Find ("Model").gameObject;

		// Get all of the renderers of the tank.
		Renderer[] renderers = m_PlayerRenderers.GetComponentsInChildren<Renderer> ();

		// Go through all the renderers...
		for (int i = 0; i < renderers.Length; i++) {
			// ... set their material color to the color specific to this tank.
			renderers [i].material.color = m_Color;
		}

		if (m_PlayerRenderers)
			m_PlayerRenderers.SetActive (false);

		m_NameText.text = "<color=#" + ColorUtility.ToHtmlStringRGB (m_Color) + ">" + m_PlayerName + "</color>";
	}

	[ClientCallback]
	public void Update() {
		if (!isLocalPlayer) {
			return;
		}

		/*if (GameManager.s_Instance.m_GameIsFinished && !m_IsReady) {
			if (Input.GetButtonDown ("Fire" + (m_LocalID + 1))) {
				CmdSetReady ();
			}
		}*/
	}

	public void SetLeader(bool leader) {
		RpcSetLeader (leader);
	}

	[ClientRpc]
	public void RpcSetLeader(bool leader) {
		m_isLeader = leader;
	}

	[Command]
	public void CmdSetReady() {
		m_IsReady = true;
	}

	public void ActivateCrown(bool active) {
		m_NameText.gameObject.SetActive (active);
	}

	public override void OnNetworkDestroy() {
		GameManager.s_Instance.RemovePlayers (gameObject);
	}
}
