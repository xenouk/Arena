using UnityEngine;
using System;

[Serializable]
public class PlayerManager {

	// This class is to manage various settings on a tank.
	// It works with the GameManager class to control how the tanks behave
	// and whether or not players have control of their tank in the 
	// different phases of the game.

	public Color m_PlayerColor;               // This is the color this tank will be tinted.
	public Transform m_SpawnPoint;            // The position and direction the tank will have when it spawns.
	[HideInInspector]
	public int m_PlayerNumber;                // This specifies which player this the manager for.
	[HideInInspector]
	public GameObject m_Instance;             // A reference to the instance of the tank when it is created.
	[HideInInspector]
	public GameObject m_Models;        // The transform that is a parent of all the tank's renderers.  This is deactivated when the tank is dead.
	[HideInInspector]
	public int m_Wins;                        // The number of wins this player has so far.
	[HideInInspector]
	public int m_Kills;                       // The number of kills this player has so far.
	[HideInInspector]
	public string m_PlayerName;               // The player name set in the lobby
	[HideInInspector]
	public int m_LocalPlayerID;               // The player localID (if there is more than 1 player on the same machine)

	public PlayerController m_Movement;       // References to various objects for control during the different game phases.
	public PlayerWeapons m_Weapons;
	public PlayerHealth m_Health;
	public PlayerSetup m_Setup;

	public void Setup() {
		// Get references to the components.
		m_Movement = m_Instance.GetComponent<PlayerController> ();
		m_Weapons = m_Instance.GetComponent<PlayerWeapons> ();
		m_Health = m_Instance.GetComponent<PlayerHealth> ();
		m_Setup = m_Instance.GetComponent<PlayerSetup> ();

		// Get references to the child objects.
		m_Models = m_Health.m_Model;

		//Set a reference to that amanger in the health script, to disable control when dying
		m_Health.m_Manager = this;
		m_Movement.m_Manager = this;
		m_Weapons.m_Manager = this;
		m_Weapons._playerController = m_Movement;

		// Set the player numbers to be consistent across the scripts.
		m_Movement.m_PlayerNumber = m_PlayerNumber;
		m_Movement.m_LocalID = m_LocalPlayerID;

		//setup is use for diverse Network Related sync
		m_Setup.m_Color = m_PlayerColor;
		m_Setup.m_PlayerName = m_PlayerName;
		m_Setup.m_PlayerNumber = m_PlayerNumber;
		m_Setup.m_LocalID = m_LocalPlayerID;
		m_Setup.m_kills = m_Kills;
	}

	// Used during the phases of the game where the player shouldn't be able to control their tank.
	public void DisableControl() {
		m_Movement.enabled = false;
		m_Weapons.enabled = false;
	}
		
	// Used during the phases of the game where the player should be able to control their tank.
	public void EnableControl() {
		m_Movement.enabled = true;
		m_Weapons.enabled = true;
	}

	public string GetName() {
		return m_Setup.m_PlayerName;
	}

	public int GetKills {
		get{ return m_Setup.m_kills; }
		set{ m_Setup.m_kills = value; }
	}

	public void SetLeader(bool leader) { 
		m_Setup.SetLeader (leader);
	}

	public bool IsReady() {
		return m_Setup.m_IsReady;
	}

	// Used at the start of each round to put the tank into it's default state.
	public void Reset() {
		m_Movement.SetDefaults ();
		m_Health.SetDefaults ();
		m_Weapons.SetDefaults ();

		if (m_Movement.hasAuthority) {
			m_Movement._playerRigidbody.position = m_SpawnPoint.position;
			m_Movement._playerRigidbody.rotation = m_SpawnPoint.rotation;
		}
	}
}