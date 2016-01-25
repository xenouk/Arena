using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Collections;

namespace UnityStandardAssets.Network
{
    public class LobbyServerList : MonoBehaviour {
        public LobbyManager lobbyManager;

        public RectTransform serverListRect;
        public GameObject serverEntryPrefab;
        public GameObject noServerFound;

        protected int currentPage = 0;
        protected int previousPage = 0;

        void OnEnable() {
			currentPage = 0;
			previousPage = 0;

			foreach (Transform t in serverListRect)
				Destroy (t.gameObject);

			noServerFound.SetActive (false);

			RequestPage (0);
		}

		public void RefreshServers(){
			foreach (Transform t in serverListRect)
				Destroy (t.gameObject);
			
			RequestPage (currentPage);
		}

        public void OnGUIMatchList(ListMatchResponse response) {
			if (response.matches.Count == 0) {
				if (currentPage == 0) {
					noServerFound.SetActive (true);
				}

				currentPage = previousPage;
               
				return;
			}

			// Clean up the List for update
			noServerFound.SetActive (false);
			foreach (Transform t in serverListRect)
				Destroy (t.gameObject);
			
			// Display the current servers
			for (int i = 0; i < response.matches.Count; ++i) {
				GameObject _server = Instantiate (serverEntryPrefab) as GameObject;
				_server.GetComponent<LobbyServerEntry> ().Populate (response.matches [i], lobbyManager);
				_server.transform.SetParent (serverListRect, false);
			}
		}

        public void ChangePage(int dir) {
			int newPage = Mathf.Max (0, currentPage + dir);

			//if we have no server currently displayed, need we need to refresh page0 first instead of trying to fetch any other page
			if (noServerFound.activeSelf)
				newPage = 0;

			RequestPage (newPage);
		}

        public void RequestPage(int page) {
			previousPage = currentPage;
			currentPage = page;
			lobbyManager.matchMaker.ListMatches (page, 6, "", OnGUIMatchList);
		}
    }
}