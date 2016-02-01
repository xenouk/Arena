using UnityEngine;
using System.Collections;
using Steamworks;

public class SteamScript : MonoBehaviour {
	private CallResult<NumberOfCurrentPlayers_t> m_NumberOfCurrentPlayers;

	void Start() {
		if(SteamManager.Initialized) {
			string name = SteamFriends.GetPersonaName();
			Debug.Log(name);
		}
	}

	private void OnEnable() {
		if (SteamManager.Initialized) {
			m_NumberOfCurrentPlayers = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);
		}
	}

	private void Update() {
		if(Input.GetKeyDown(KeyCode.Space)) {
			SteamAPICall_t handle = SteamUserStats.GetNumberOfCurrentPlayers();
			m_NumberOfCurrentPlayers.Set(handle);
			Debug.Log("Called GetNumberOfCurrentPlayers()");
		}
	}

	private void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t pCallback, bool bIOFailure) {
		if (pCallback.m_bSuccess != 1 || bIOFailure) {
			Debug.Log("There was an error retrieving the NumberOfCurrentPlayers.");
		}
		else {
			Debug.Log("The number of players playing your game: " + pCallback.m_cPlayers);
		}
	}
}
