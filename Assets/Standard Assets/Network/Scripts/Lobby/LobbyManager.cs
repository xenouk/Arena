using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections;
using System;

namespace UnityStandardAssets.Network
{
    public class LobbyManager : NetworkLobbyManager {
        static public LobbyManager s_Singleton;

        [Tooltip("The minimum number of players in the lobby before player can be ready")]
        public int minPlayer;

        public LobbyTopPanel topPanel;

        public RectTransform mainMenuPanel;
        public RectTransform lobbyPanel;

        public LobbyInfoPanel infoPanel;

        protected RectTransform currentPanel;

        public Button backButton;

        public Text statusInfo;
        public Text hostInfo;
		public Text matchModeInfo;
		public Texture2D m_Cursor;

        //used to disconnect a client properly when exiting the matchmaker
        public bool isMatchmaking = false;
        protected bool _disconnectServer = false;

		protected ulong _currentMatchID;
		protected ulong _currentNodeID;

        protected LobbyHook _lobbyHooks;

		public int currentMatchValue;
		public string[] matchModes = new string[] {"DEAD MATCH"," ROUND MATCH"};

        void Awake() {
			if (FindObjectsOfType<LobbyManager> ().Length > 1)
				Destroy (gameObject);
		}

        void Start() {
			s_Singleton = this;
			_lobbyHooks = GetComponent<UnityStandardAssets.Network.LobbyHook> ();
			currentPanel = mainMenuPanel;

			backButton.gameObject.SetActive (false);
			GetComponent<Canvas> ().enabled = true;

			DontDestroyOnLoad (gameObject);

			SetServerInfo ("Offline", "None");

		}

        public override void OnLobbyClientSceneChanged(NetworkConnection conn) {
			if (!conn.playerControllers [0].unetView.isLocalPlayer)
				return;

			if (SceneManager.GetActiveScene ().name == lobbyScene) {
				if (topPanel.isInGame) {
					ChangeTo (lobbyPanel);
					if (isMatchmaking) {
						if (conn.playerControllers [0].unetView.isServer) {
							backDelegate = StopHostToMenu;
						} else {
							backDelegate = QuitLobbyToMenu;
						}
					} else {
						if (conn.playerControllers [0].unetView.isClient) {
							backDelegate = StopHostToMenu;
						} else {
							backDelegate = QuitLobbyToMenu;
						}
					}
				} else {
					ChangeTo (mainMenuPanel);
				}

				topPanel.ToggleVisibility (true);
				topPanel.isInGame = false;
			} else {
				ChangeTo (null);
				Destroy (GameObject.Find ("MainMenuUI(Clone)"));

				backDelegate = StopGameClbk;
				topPanel.isInGame = true;
				topPanel.ToggleVisibility (false);
			}
		}
		// Chnage Panels
        public void ChangeTo(RectTransform newPanel) {
			if (currentPanel != null) {
				currentPanel.gameObject.SetActive (false);
			}

			if (newPanel != null) {
				newPanel.gameObject.SetActive (true);
			}

			currentPanel = newPanel;

			if (currentPanel != mainMenuPanel) {
				backButton.gameObject.SetActive (true);
			} else {
				backButton.gameObject.SetActive (false);
				SetServerInfo ("Offline", "None");
				isMatchmaking = false;
			}
		}

        public void DisplayIsConnecting() {
			var _this = this;
			infoPanel.Display ("Connecting...", "Cancel", () => {
				_this.backDelegate ();
			});
		}

        public void SetServerInfo(string status, string host) {
			statusInfo.text = status;
			hostInfo.text = host;
		}

		public void ExitGame() {
			Application.Quit ();
		}


        public delegate void BackButtonDelegate();
        public BackButtonDelegate backDelegate;
        public void GoBackButton() {
			backDelegate ();
		}
			
		/**
         * 
         *  Server management 
         * 
         **/
        public void BackToMenu() {
			ChangeTo (mainMenuPanel);
		}
		/**
		 * When the host leaves the lobby
		 **/
        public void StopHostToMenu() {
			if (isMatchmaking) {
				this.matchMaker.DestroyMatch ((NetworkID)_currentMatchID, OnMatchDestroyed);
				_disconnectServer = true;
			} else {
				StopHost ();
			}
            
			ChangeTo (mainMenuPanel);
		}

		/**
		 * When the client leaves the lobby
		 **/
        public void QuitLobbyToMenu() {
			StopClient ();
			if (isMatchmaking) {
				StopMatchMaker ();
			}

			ChangeTo (mainMenuPanel);
		}

		/**
		 * When the host leaves the lobby (Local)
		 **/
        public void StopServerClbk() {
			StopServer ();
			ChangeTo (mainMenuPanel);
		}

		/**
		 * When the game finish and return to lobby
		 **/
        public void StopGameClbk() {
			SendReturnToLobby ();
			ChangeTo (lobbyPanel);
		}

        /**
         *  Start Host a Game 
         **/
        public override void OnStartHost() {
			base.OnStartHost ();

			ChangeTo (lobbyPanel);
			backDelegate = StopHostToMenu;
			SetServerInfo ("Hosting", networkAddress);

			matchModeInfo.text = "MODE: "+ matchModes[currentMatchValue];
		}

		/**
         * 
         *  When this Client Connects/Creates a Server 
         * 
         **/
        public override void OnClientConnect(NetworkConnection conn) {
			base.OnClientConnect (conn);

			infoPanel.gameObject.SetActive (false);

			if (!NetworkServer.active) {//only to do on pure client (not self hosting client)
				ChangeTo (lobbyPanel);
				backDelegate = QuitLobbyToMenu;
				SetServerInfo ("Client", networkAddress);
			}
		}

		/**
         *  When this Client create a Server 
         **/
		public override void OnMatchCreate(CreateMatchResponse matchInfo) {
			base.OnMatchCreate (matchInfo);
			_currentMatchID = (System.UInt64)matchInfo.networkId;
		}

		//###################### Mine ###############################
		/**
         *  When this Client joins a Server
         **/
		public void OnMatchJoined(JoinMatchResponse matchInfo){
			//base.OnMatchJoined (matchInfo);
			_currentMatchID = (System.UInt64)matchInfo.networkId;
			_currentNodeID = (System.UInt64)matchInfo.nodeId;
			matchModeInfo.text = "MODE: "+ matchModes[currentMatchValue];

			if (matchInfo.success) { 
				try { 
					UnityEngine.Networking.Utility.SetAccessTokenForNetwork(matchInfo.networkId, new NetworkAccessToken (matchInfo.accessTokenString)); 
				} catch (Exception ex) { 
					//if (LogFilter.logError) { 
					//	Debug.LogError (ex); 
					//} 
				}
				StartClient(new MatchInfo(matchInfo));
			} else if (LogFilter.logError) { 
				Debug.LogError (string.Concat ("Join Failed:", matchInfo)); 
			} 
		}

		/**
         *  When a match is destroyed 
         **/
        public void OnMatchDestroyed(BasicResponse resp) {
			if (_disconnectServer) {
				StopMatchMaker ();
				StopHost ();
			}
		}

		/**
		 *  Server callbacks
		 **/

        //We want to disable the button JOIN if we don't have enough player
        //But OnLobbyClientConnect isn't called on hosting player. So we override the lobbyPlayer creation
        public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId) {
			GameObject obj = Instantiate (lobbyPlayerPrefab.gameObject) as GameObject;

			LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer> ();
			newPlayer.RpcToggleJoinButton (numPlayers + 1 >= minPlayer);

			for (int i = 0; i < numPlayers; ++i) {
				LobbyPlayer p = lobbySlots [i] as LobbyPlayer;

				if (p != null) {
					p.RpcToggleJoinButton (numPlayers + 1 >= minPlayer);
				}
			}
				
			return obj;
		}

        public override void OnLobbyServerDisconnect(NetworkConnection conn) {
			for (int i = 0; i < numPlayers; ++i) {
				LobbyPlayer p = lobbySlots [i] as LobbyPlayer;

				if (p != null) {
					p.RpcToggleJoinButton (numPlayers >= minPlayer);
				}
			}
		}

		/**
		 *This hook allows you to apply state data from the lobby-player to the game-player.
		 *Transfer player information to GameManager.
		 **/
        public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer) {
			if (_lobbyHooks)
				_lobbyHooks.OnLobbyServerSceneLoadedForPlayer (this, lobbyPlayer, gamePlayer, currentMatchValue);

			return true;
		}

        static protected float _matchStartCountdown = 5.0f;
		/**
		 *  When all players are ready
		 **/
        public override void OnLobbyServerPlayersReady() {
			bool temp = true;

			for (int i = 0; i < numPlayers; ++i) {
				LobbyPlayer p = lobbySlots [i] as LobbyPlayer;

				if (p != null) {
					if (!p.readyToBegin) {
						temp = false;
						break;
					}
				}
			}
			// Start the countdown
			if(temp)
				StartCoroutine (ServerCountdownCoroutine ());
		}

		/**
		 *  Countdown for the game to start 
		 **/
        public IEnumerator ServerCountdownCoroutine() {
			float remainingTime = _matchStartCountdown;
			int floorTime = Mathf.FloorToInt (remainingTime);

			while (remainingTime > 0) {
				yield return null;

				remainingTime -= Time.deltaTime;
				int newFloorTime = Mathf.FloorToInt (remainingTime);

				if (newFloorTime != floorTime) {//to avoid flooding the network of message, we only send a notice to cleint when the number of second do change.
					floorTime = newFloorTime;

					for (int i = 0; i < lobbySlots.Length; ++i) {
						if (lobbySlots [i] != null) {//there is max player slot, so some could be == null, need to test it ebfore accessing!
							(lobbySlots [i] as LobbyPlayer).RpcUpdateCountdown (floorTime);
						}
					}
				}
			}

			for (int i = 0; i < lobbySlots.Length; ++i) {
				if (lobbySlots [i] != null) {
					(lobbySlots [i] as LobbyPlayer).RpcUpdateCountdown (0);
				}
			}
			
			ServerChangeScene (playScene);
		}

		/**
		 *  Client callbacks -
		 **/
        public override void OnClientDisconnect(NetworkConnection conn) {
			base.OnClientDisconnect (conn);
			ChangeTo (mainMenuPanel);
		}

        public override void OnClientError(NetworkConnection conn, int errorCode) {
			ChangeTo (mainMenuPanel);
			infoPanel.Display ("Cient error : " + (errorCode == 6 ? "timeout" : errorCode.ToString ()), "Close", null);
		}

		void OnLevelWasLoaded(int level){
			if (level == 0)
				Cursor.SetCursor (null, Vector2.zero, CursorMode.Auto);
			else
				Cursor.SetCursor (m_Cursor, Vector2.zero, CursorMode.Auto);
		}

		void FixedUpdate () {
			//if (!mainMenuPanel.gameObject.activeInHierarchy)
				//matchMaker.ListMatches (0, 6, "", GetMatchList);
		}

		public void GetMatchList(ListMatchResponse response) {
			if(response.matches.Count > 0)
				print (Int32.Parse(response.matches [0].name.Split(' ')[response.matches [0].name.Split(' ').Length-1]));
		}
    }
}
