using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoundMatchManager {

	public IEnumerator RoundPlaying() {
		//notify clients that the round is now started, they should allow player to move.
		GameManager.s_Instance.RpcRoundPlaying ();

		// While there is not one tank left...
		while (!OneTankLeft ()) {
			// ... return on the next frame.
			yield return null;
		}
	}

	public IEnumerator RoundEnding() {
		// Clear the winner from the previous round.
		GameManager.s_Instance.m_RoundWinner = null;

		// See if there is a winner now the round is over.
		GameManager.s_Instance.m_RoundWinner = GetRoundWinner ();

		// If there is a winner, increment their score.
		if (GameManager.s_Instance.m_RoundWinner != null)
			GameManager.s_Instance.m_RoundWinner.m_Wins++;

		// Now the winner's score has been incremented, see if someone has won the game.
		GameManager.s_Instance.m_GameWinner = GetGameWinner ();

		GameManager.s_Instance.RpcUpdateMessage (EndMessage (0));

		//notify client they should disable tank control
		GameManager.s_Instance.RpcRoundEnding ();

		// Wait for the specified length of time until yielding control back to the game loop.
		yield return GameManager.s_Instance.m_EndWait;
	}

	// This is used to check if there is one or fewer tanks remaining and thus the round should end.
	public bool OneTankLeft() {
		// Start the count of tanks left at zero.
		int numTanksLeft = 0;

		// Go through all the tanks...
		for (int i = 0; i < GameManager.m_Players.Count; i++) {
			// ... and if they are active, increment the counter.
			if (GameManager.m_Players [i].m_Models.activeSelf)
				numTanksLeft++;
		}
		// If there are one or fewer tanks remaining return true, otherwise return false.
		return numTanksLeft <= 1;
	}

	// This function is to find out if there is a winner of the round.
	// This function is called with the assumption that 1 or fewer tanks are currently active.
	public PlayerManager GetRoundWinner() {
		// Go through all the tanks...
		for (int i = 0; i < GameManager.m_Players.Count; i++) {
			// ... and if one of them is active, it is the winner so return it.
			if (GameManager.m_Players [i].m_Models.activeSelf)
				return GameManager.m_Players [i];
		}

		// If none of the tanks are active it is a draw so return null.
		return null;
	}

	// This function is to find out if there is a winner of the game.
	public PlayerManager GetGameWinner() {
		int maxScore = 0;

		// Go through all the tanks...
		for (int i = 0; i < GameManager.m_Players.Count; i++) {
			if (GameManager.m_Players [i].m_Wins > maxScore) {
				maxScore = GameManager.m_Players [i].m_Wins;
			}

			// ... and if one of them has enough rounds to win the game, return it.
			if (GameManager.m_Players [i].m_Wins == GameManager.s_Instance.m_NumRoundsToWin)
				return GameManager.m_Players [i];
		}

		//go throught a second time to enable/disable the crown on tanks
		//(note : we don't enter it if the maxScore is 0, as no one is current leader yet!)
		for (int i = 0; i < GameManager.m_Players.Count && maxScore > 0; i++) {
			GameManager.m_Players [i].SetLeader (maxScore == GameManager.m_Players [i].m_Wins);
		}

		// If no tanks have enough rounds to win, return null.
		return null;
	}

	// Returns a string of each player's score in their tank's color.
	public string EndMessage(int waitTime) {
		// By default, there is no winner of the round so it's a draw.
		string message = "DRAW!";

		// If there is a game winner set the message to say which player has won the game.
		if (GameManager.s_Instance.m_GameWinner != null)
			message = "<color=#" + ColorUtility.ToHtmlStringRGB (GameManager.s_Instance.m_GameWinner.m_PlayerColor) + ">" + GameManager.s_Instance.m_GameWinner.m_PlayerName + "</color> WINS THE GAME!";
		// If there is a winner, change the message to display 'PLAYER #' in their color and a winning message.
		else if (GameManager.s_Instance.m_RoundWinner != null)
			message = "<color=#" + ColorUtility.ToHtmlStringRGB (GameManager.s_Instance.m_RoundWinner.m_PlayerColor) + ">" + GameManager.s_Instance.m_RoundWinner.m_PlayerName + "</color> WINS THE ROUND!";

		// After either the message of a draw or a winner, add some space before the leader board.
		message += "\n\n";

		// Go through all the tanks and display their scores with their 'PLAYER #' in their color.
		for (int i = 0; i < GameManager.m_Players.Count; i++) {
			message += "<color=#" + ColorUtility.ToHtmlStringRGB (GameManager.m_Players [i].m_PlayerColor) + ">" + GameManager.m_Players [i].m_PlayerName + "</color>: " + GameManager.m_Players [i].m_Wins + " WINS "
				+ (GameManager.m_Players [i].IsReady () ? "<size=15>READY</size>" : "") + " \n";
		}

		if (GameManager.s_Instance.m_GameWinner != null)
			message += "\n\n<size=20 > Return to lobby in " + waitTime + "\n</size>";

		return message;
	}
}
