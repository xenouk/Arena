using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking.Match;

namespace UnityStandardAssets.Network
{
    //Main menu, mainly only a bunch of callback called by the UI (setup throught the Inspector)
    public class LobbyMainMenu : MonoBehaviour {
        public LobbyManager lobbyManager;

        public RectTransform lobbyServerList;
        public RectTransform lobbyPanel;

        public InputField ipInput;
        public InputField matchNameInput;
		public Dropdown matchMode;

        public void OnEnable() {
			lobbyManager.topPanel.ToggleVisibility (true);

			ipInput.onEndEdit.RemoveAllListeners ();
			ipInput.onEndEdit.AddListener (onEndEditIP);

			matchNameInput.onEndEdit.RemoveAllListeners ();
			matchNameInput.onEndEdit.AddListener (onEndEditGameName);
		}

        public void OnClickHost() {
			lobbyManager.StartHost ();
		}

        public void OnClickJoin() {
			lobbyManager.ChangeTo (lobbyPanel);

			lobbyManager.networkAddress = ipInput.text;
			lobbyManager.StartClient ();

			lobbyManager.backDelegate = lobbyManager.QuitLobbyToMenu;
			lobbyManager.DisplayIsConnecting ();

			lobbyManager.SetServerInfo ("Connecting...", lobbyManager.networkAddress);
		}

        public void OnClickDedicated() {
			lobbyManager.ChangeTo (null);
			lobbyManager.StartServer ();

			lobbyManager.backDelegate = lobbyManager.StopServerClbk;

			lobbyManager.SetServerInfo ("Dedicated Server", lobbyManager.networkAddress);
		}
		// Click to create a match name
        public void OnClickCreateMatchmakingGame() {
			lobbyManager.StartMatchMaker ();
			CreateMatchRequest newMatch = new CreateMatchRequest ();
			newMatch.name = matchNameInput.text + " "+matchMode.value.ToString();
			newMatch.size = (uint)lobbyManager.maxPlayers;
			newMatch.advertise = true;
			newMatch.password = "";
			lobbyManager.matchMaker.CreateMatch (
				newMatch,
				lobbyManager.OnMatchCreate);

			lobbyManager.backDelegate = lobbyManager.StopHost;
			lobbyManager.isMatchmaking = true;
			lobbyManager.DisplayIsConnecting ();
			lobbyManager.currentMatchValue = matchMode.value;

			lobbyManager.SetServerInfo ("Matchmaker Host", lobbyManager.matchHost);
		}
		// Open the Server list to find games
        public void OnClickOpenServerList() {
			lobbyManager.StartMatchMaker ();
			lobbyManager.backDelegate = lobbyManager.BackToMenu;
			lobbyManager.ChangeTo (lobbyServerList);
		}

        void onEndEditIP(string text) {
			if (Input.GetKeyDown (KeyCode.Return)) {
				OnClickJoin ();
			}
		}

        void onEndEditGameName(string text) {
			if (Input.GetKeyDown (KeyCode.Return)) {
				OnClickCreateMatchmakingGame ();
			}
		}
    }
}
