using UnityEngine;
using System.Collections;

public class DeadMatchManager {	
	public IEnumerator DeadMatchPlaying() {
		//notify clients that the round is now started, they should allow player to move.
		GameManager.s_Instance.RpcRoundPlaying ();
		GameManager.s_Instance.RpcSetupStatus (true);
		// While there is not one tank left...
		while (!MaxKill ()) {
			GameManager.s_Instance.RpcUpdateStatus ();
			// ... return on the next frame.
			yield return null;
		}
	}

	public bool MaxKill(){
		// Go through all the tanks...
		for (int i = 0; i < GameManager.m_Players.Count; i++) {
			// ... and a tank got max kills then return true
			if (GameManager.m_Players [i].GetKills > 9) {
				GameManager.s_Instance.m_GameWinner = GameManager.m_Players [i];
				return true;
			}
		}

		return false;
	}

	public IEnumerator RoundEnding() {
		GameManager.s_Instance.RpcSetupStatus (false);
		GameManager.s_Instance.RpcUpdateMessage (EndMessage (0));

		//notify client they should disable tank control
		GameManager.s_Instance.RpcRoundEnding ();

		// Wait for the specified length of time until yielding control back to the game loop.
		yield return GameManager.s_Instance.m_EndWait;
	}

	// Returns a string of each player's score in their tank's color.
	public string EndMessage(int waitTime) {
		string message = "";

		// If there is a game winner set the message to say which player has won the game.
		if (GameManager.s_Instance.m_GameWinner != null)
			message = "<color=#" + ColorUtility.ToHtmlStringRGB (GameManager.s_Instance.m_GameWinner.m_PlayerColor) + ">" + GameManager.s_Instance.m_GameWinner.m_PlayerName + "</color> WINS THE GAME!";

		// After either the message of a draw or a winner, add some space before the leader board.
		message += "\n\n";

		// Go through all the tanks and display their scores with their 'PLAYER #' in their color.
		for (int i = 0; i < GameManager.m_Players.Count; i++) {
			message += "<color=#" + ColorUtility.ToHtmlStringRGB (GameManager.m_Players [i].m_PlayerColor) + ">" + GameManager.m_Players [i].m_PlayerName + "</color>: " + GameManager.m_Players [i].GetKills + " Kills "
				+ (GameManager.m_Players [i].IsReady () ? "<size=15>READY</size>" : "") + " \n";
		}

		if (GameManager.s_Instance.m_GameWinner != null)
			message += "\n\n<size=20 > Return to lobby in " + waitTime + "</size>";

		return message;
	}
}
