using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityStandardAssets.Network;

public class GameManager : NetworkBehaviour {

	static public GameManager s_Instance;

	//this is static so tank can be added even without the scene loaded (i.e. from lobby)
	// A collection of managers for enabling and disabling different aspects of the tanks.
	static public List<PlayerManager> m_Players = new List<PlayerManager>();  
	static public int m_MatchMode = 1;

	public int m_NumRoundsToWin = 5;          // The number of rounds a single player has to win to win the game.
	public float m_StartDelay = 3f;           // The delay between the start of RoundStarting and RoundPlaying phases.
	public float m_EndDelay = 3f;             // The delay between the end of RoundPlaying and RoundEnding phases.
	public CameraControl m_CameraControl;     // Reference to the CameraControl script for control during different phases.
	public Text m_MessageText;                // Reference to the overlay Text to display winning text, etc.
	public GameObject m_PlayerPrefab;           // Reference to the prefab the players will control.

	public Transform[] m_StartPoints;
	public GameObject m_SpawnPoints;
	public GameObject[] m_PickupPrefabs;

	[HideInInspector]
	[SyncVar]
	public bool m_GameIsFinished = false;

	//Various UI references to hide the screen between rounds.
	[Space]
	[Header("UI")]
	public CanvasGroup m_FadingScreen;  
	public CanvasGroup m_EndRoundScreen;
	public GameObject[] m_StatusPanels;
	public Text[] m_StatusNames;
	public Text[] m_StatusKills;

	[HideInInspector]
	public int m_RoundNumber;                  // Which round the game is currently on.
	[HideInInspector]
	public WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
	[HideInInspector]
	public WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
	[HideInInspector]
	public PlayerManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
	[HideInInspector]
	public PlayerManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.

	private DeadMatchManager m_DeadMatchManager;
	private RoundMatchManager m_RoundMatchManager;

	void Awake() {
		s_Instance = this;
	}

	[ServerCallback]
	private void Start() {
		m_DeadMatchManager = new DeadMatchManager ();
		m_RoundMatchManager = new RoundMatchManager ();
		// Create the delays so they only have to be made once.
		m_StartWait = new WaitForSeconds (m_StartDelay);
		m_EndWait = new WaitForSeconds (m_EndDelay);

		// Once the tanks have been created and the camera is using them as targets, start the game.
		switch (m_MatchMode) {
		case 0:
			StartCoroutine (PlayDeadMatch ());
			break;
		case 1:
			StartCoroutine (PlayRoundMatch ());
			break;
		}
	}

	public override void OnStartClient() {
		base.OnStartClient ();

		foreach (GameObject obj in m_PickupPrefabs) {
			ClientScene.RegisterPrefab (obj);
		}
	}

	/// <summary>
	/// Add a tank from the lobby hook
	/// </summary>
	/// <param name="tank">The actual GameObject instantiated by the lobby, which is a NetworkBehaviour</param>
	/// <param name="playerNum">The number of the player (based on their slot position in the lobby)</param>
	/// <param name="c">The color of the player, choosen in the lobby</param>
	/// <param name="name">The name of the Player, choosen in the lobby</param>
	/// <param name="localID">The localID. e.g. if 2 player are on the same machine this will be 1 & 2</param>
	static public void AddPlayer(GameObject player, int playerNum, Color c, string name, int localID) {
		PlayerManager tmp = new PlayerManager ();
		tmp.m_Instance = player;
		tmp.m_PlayerNumber = playerNum;
		tmp.m_PlayerColor = c;
		tmp.m_PlayerName = name;
		tmp.m_LocalPlayerID = localID;
		tmp.Setup ();

		m_Players.Add (tmp);
	}

	public void RemovePlayers(GameObject player) {
		PlayerManager toRemove = null;
		foreach (var tmp in m_Players) {
			if (tmp.m_Instance == player) {
				toRemove = tmp;
				break;
			}
		}

		if (toRemove != null)
			m_Players.Remove (toRemove);
	}

	// This is called from start and will run each phase of the game one after another. ONLY ON SERVER (as Start is only called on server)
	private IEnumerator PlayRoundMatch() {
		while (m_Players.Count < 2)
			yield return null;

		//wait to be sure that all are ready to start
		yield return new WaitForSeconds (2.0f);

		// Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
		yield return StartCoroutine (RoundStarting ());

		// Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
		yield return StartCoroutine (m_RoundMatchManager.RoundPlaying ());

		// Once execution has returned here, run the 'RoundEnding' coroutine.
		yield return StartCoroutine (m_RoundMatchManager.RoundEnding ());

		// This code is not run until 'RoundEnding' has finished.  At which point, check if there is a winner of the game.
		if (m_GameWinner != null) {// If there is a game winner, wait for certain amount or all player confirmed to start a game again
			m_GameIsFinished = true;
			float leftWaitTime = 10.0f;
			bool allAreReady = false;
			int flooredWaitTime = 10;

			while (leftWaitTime > 0.0f && !allAreReady) {
				yield return null;

				allAreReady = true;
				foreach (var tmp in m_Players) {
					allAreReady &= tmp.IsReady ();
				}

				leftWaitTime -= Time.deltaTime;

				int newFlooredWaitTime = Mathf.FloorToInt (leftWaitTime);

				if (newFlooredWaitTime != flooredWaitTime) {
					flooredWaitTime = newFlooredWaitTime;
					string message = m_RoundMatchManager.EndMessage (flooredWaitTime);
					RpcUpdateMessage (message);
				}
			}

			LobbyManager.s_Singleton.ServerReturnToLobby ();
		} else {
			// If there isn't a winner yet, restart this coroutine so the loop continues.
			// Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
			StartCoroutine (PlayRoundMatch ());
		}
	}

	private IEnumerator PlayDeadMatch() {
		while (m_Players.Count < 2)
			yield return null;

		//wait to be sure that all are ready to start
		yield return new WaitForSeconds (2.0f);

		// Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
		yield return StartCoroutine (RoundStarting ());

		// Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
		yield return StartCoroutine (m_DeadMatchManager.DeadMatchPlaying ());

		// Once execution has returned here, run the 'RoundEnding' coroutine.
		yield return StartCoroutine (m_DeadMatchManager.RoundEnding ());

		m_GameIsFinished = true;
		float leftWaitTime = 10.0f;
		bool allAreReady = false;
		int flooredWaitTime = 10;

		while (leftWaitTime > 0.0f && !allAreReady) {
			yield return null;

			allAreReady = true;
			foreach (var tmp in m_Players) {
				allAreReady &= tmp.IsReady ();
			}

			leftWaitTime -= Time.deltaTime;

			int newFlooredWaitTime = Mathf.FloorToInt (leftWaitTime);

			if (newFlooredWaitTime != flooredWaitTime) {
				flooredWaitTime = newFlooredWaitTime;
				string message = m_DeadMatchManager.EndMessage (flooredWaitTime);
				RpcUpdateMessage (message);
			}
		}

		LobbyManager.s_Singleton.ServerReturnToLobby ();
	}

	public IEnumerator RoundStarting() {
		//we notify all clients that the round is starting
		RpcRoundStarting ();

		// Wait for the specified length of time until yielding control back to the game loop.
		yield return m_StartWait;
	}

	public IEnumerator ClientRoundStartingFade() {
		float elapsedTime = 0.0f;
		float wait = m_StartDelay - 1f;

		yield return null;

		while (elapsedTime < wait) {
			if (m_RoundNumber == 1)
				m_FadingScreen.alpha = 1.0f - (elapsedTime / wait);
			else
				m_EndRoundScreen.alpha = 1.0f - (elapsedTime / wait);

			elapsedTime += Time.deltaTime;

			//sometime, synchronization lag behind because of packet drop, so we make sure our tank are reseted
			if (elapsedTime / wait < 0.5f)
				ResetAllPlayers ();

			yield return null;
		}
	}

	private IEnumerator SpawnPickups(){
		const float MIN_TIME = 2.0f;
		const float MAX_TIME = 3.0f;

		while (!m_GameIsFinished) {
			yield return new WaitForSeconds(Random.Range(MIN_TIME, MAX_TIME));

			Transform spot;

			do {
				spot = m_SpawnPoints.transform.GetChild(Random.Range(0, m_SpawnPoints.transform.childCount));
			} while(spot.childCount > 0);

			GameObject pickup = Instantiate (
				m_PickupPrefabs [Random.Range (0, m_PickupPrefabs.Length)], 
				spot.position, 
				Quaternion.identity) as GameObject;
			pickup.transform.SetParent (spot);
			NetworkServer.Spawn (pickup);
		}
	}

	[ClientRpc]
	public void RpcRoundStarting() {
		// As soon as the round starts reset the tanks and make sure they can't move.
		ResetAllPlayers ();
		DisablePlayerControl ();

		// Snap the camera's zoom and position to something appropriate for the reset tanks.
		m_CameraControl.SetAppropriatePositionAndSize ();
		m_RoundNumber++;

		switch (m_MatchMode) {
		case 0:
			m_MessageText.text = "DEAD MATCH!";
			break;
		case 1:
			// Increment the round number and display text showing the players what round it is.
			m_MessageText.text = "ROUND " + m_RoundNumber;
			break;
		}
		if(isServer)
			StartCoroutine (SpawnPickups ());
		StartCoroutine (ClientRoundStartingFade ());
	}

	[ClientRpc]
	public void RpcSetupStatus(bool active){
		for (int i = 0; i < m_Players.Count; i++) {
			m_StatusPanels[i].SetActive (active);
			m_StatusNames[i].text = m_Players[i].GetName();
		}
	}

	[ClientRpc]
	public void RpcUpdateStatus(){
		for (int i = 0; i < m_Players.Count; i++) {
			m_StatusKills[i].text = m_Players[i].GetKills.ToString ();
		}
	}

	[ClientRpc]
	public void RpcRoundPlaying() {
		// As soon as the round begins playing let the players control the tanks.
		EnablePlayerControl ();

		// Clear the text from the screen.
		m_MessageText.text = string.Empty;
	}

	[ClientRpc]
	public void RpcRoundEnding() {
		DisablePlayerControl ();
		StartCoroutine (ClientRoundEndingFade ());
	}

	[ClientRpc]
	public void RpcUpdateMessage(string msg) {
		m_MessageText.text = msg;
	}

	public IEnumerator ClientRoundEndingFade() {
		float elapsedTime = 0.0f;
		float wait = m_EndDelay;
		while (elapsedTime < wait) {
			m_EndRoundScreen.alpha = (elapsedTime / wait);

			elapsedTime += Time.deltaTime;
			yield return null;
		}
	}


	// This function is used to turn all the tanks back on and reset their positions and properties.
	public void ResetAllPlayers() {
		for (int i = 0; i < m_Players.Count; i++) {
			m_Players [i].m_SpawnPoint = m_StartPoints [m_Players [i].m_Setup.m_PlayerNumber];
			m_Players [i].Reset ();
		}
	}


	public void EnablePlayerControl() {
		for (int i = 0; i < m_Players.Count; i++) {
			m_Players [i].EnableControl ();
		}
	}


	public void DisablePlayerControl() {
		for (int i = 0; i < m_Players.Count; i++) {
			m_Players [i].DisableControl ();
		}
	}
}
