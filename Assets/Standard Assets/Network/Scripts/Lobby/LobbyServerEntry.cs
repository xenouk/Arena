﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using System.Collections;
using System;

namespace UnityStandardAssets.Network
{
    public class LobbyServerEntry : MonoBehaviour {
        public Text serverInfoText;
        public Text slotInfo;
        public Button joinButton;


        public void Populate(MatchDesc match, LobbyManager lobbyManager) {
			serverInfoText.text = match.name;

			slotInfo.text = match.currentSize.ToString () + "/" + match.maxSize.ToString ();

			NetworkID networkID = match.networkId;

			joinButton.onClick.RemoveAllListeners ();
			joinButton.onClick.AddListener (() => {
				JoinMatch (networkID, lobbyManager, match);
			});
		}

        void JoinMatch(NetworkID networkID, LobbyManager lobbyManager, MatchDesc match) {
			lobbyManager.DisplayIsConnecting ();
			lobbyManager.matchMaker.JoinMatch (networkID, "", lobbyManager.OnMatchJoined);
			lobbyManager.currentMatchValue = Int32.Parse(match.name.Split(' ')[match.name.Split(' ').Length-1]);
			lobbyManager.backDelegate = lobbyManager.QuitLobbyToMenu;
			lobbyManager.isMatchmaking = true;
		}
    }
}