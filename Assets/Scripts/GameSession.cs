﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameSession : Photon.MonoBehaviour {

	public Text healthUI;
	public Text killsUI;
	public Text deathsUI;

	private static PhotonView ScenePhotonView;
	
	public enum PlayerState {
		Default,
		Invincible
	};
	
	[System.Serializable]
	public class PlayerInfo {
		public float health;
		public int kills;
		public int deaths;
		public PlayerState state = PlayerState.Default;
		
		public PlayerInfo (float h, int k, int d) {
			health = h;
			kills = k;
			deaths = d;
		}
	};
	
	[System.Serializable]
	public class PlayerUpdateMessage {
		public bool connected;
		public float newHealth;
		public int newKills;
		public int newDeaths;
		public PlayerState newState;
		
		public PlayerUpdateMessage(bool c, float h, int k, int d, PlayerState s) {
			connected = c;
			newHealth = h;
			newKills = k;
			newDeaths = d;
			newState = s;			
		}
	}
	
	private float maxHealth = 5f;
	
	private Dictionary<int, PlayerInfo> localFrags = new Dictionary<int,PlayerInfo>();

	// Use this for initialization
	void Awake () {
		ScenePhotonView = this.GetComponent<PhotonView>();
	}
	
	//Only should be called called when being master client
	public void NewPlayerConnected (int playerID)
	{
		Debug.Log("New player connected to host "+playerID);
		if (PhotonNetwork.isMasterClient)
		{
			if (!localFrags.ContainsKey(PhotonNetwork.player.ID)) // Lel
			{
				ScenePhotonView.RPC("NewPlayerInfo", PhotonTargets.All, PhotonNetwork.player.ID, new PlayerInfo(maxHealth, 0, 0));
			}
		
			foreach (KeyValuePair<int, PlayerInfo> entry in localFrags) {
				ScenePhotonView.RPC("NewPlayerInfo", PhotonTargets.Others, entry.Key, entry.Value);
			}
		
			ScenePhotonView.RPC("NewPlayerInfo", PhotonTargets.All, playerID, new PlayerInfo(maxHealth, 0, 0));
		}
	}
	
	//Only should be called called when being master client
	public void PlayerDisconnected (int playerID)
	{
		Debug.Log("New player disconnected to host "+playerID);
		if (PhotonNetwork.isMasterClient) {			
			ScenePhotonView.RPC("PlayerInfoUpdate", PhotonTargets.All, playerID, false, 0.0f, 0, 0);
		}
	}
	
	[RPC]
	void NewPlayerInfo (int playerID, PlayerInfo newPlayer)
	{
		if (!localFrags.ContainsKey(playerID))
		{
			Debug.Log("New player info registered");
			localFrags.Add (playerID, newPlayer);
		}
	}
	
	[RPC]
	void PlayerInfoUpdate(int playerID, bool connected, float newHealth, int newKills, int newDeaths)
	{
		if (!connected) // Disconnect
		{
			Debug.Log("Player Disconnected "+playerID);
			localFrags.Remove(playerID);
		}
		else
		{
			Debug.Log("Player Update "+playerID);
			if (localFrags.ContainsKey(playerID))
			{
				localFrags[playerID].health += newHealth;
				localFrags[playerID].kills += newKills;
				localFrags[playerID].deaths += newDeaths;
			}
			else
			{
				Debug.Log("Hwat");
			}
		}
	}
	
	[RPC]
	void NewFrag (int fromPlayer, int toPlayer, float howMuch)
	{
		if (PhotonNetwork.isMasterClient)
		{
			if (localFrags.ContainsKey(fromPlayer) && localFrags.ContainsKey(toPlayer))
			{
				ScenePhotonView.RPC("PlayerInfoUpdate", PhotonTargets.All, fromPlayer, 	true, 	0.0f, 			0, 	0);
				ScenePhotonView.RPC("PlayerInfoUpdate", PhotonTargets.All, toPlayer, 	true, 	-howMuch, 	0, 	0);
				
				if (localFrags[toPlayer].health <= 0) {
					ScenePhotonView.RPC("PlayerInfoUpdate", PhotonTargets.All, 	fromPlayer, true, 	0.0f, 			1,	0);
					ScenePhotonView.RPC("PlayerInfoUpdate", PhotonTargets.All, 	toPlayer, 	true, 	maxHealth, 	0,	1);
					ScenePhotonView.RPC("PlayerDead", PhotonTargets.All, toPlayer);
				}
			}
			else
			{
				Debug.Log("Twaht");
			}
		}
	}
	
	[RPC]
	void PlayerDead (int deadPlayer)
	{
		// Explosion
		
		if (deadPlayer == PhotonNetwork.player.ID)
		{ 
			PlayerNerworkInstance netInst = GameObject.Find("Control").GetComponent<PlayerNerworkInstance>();
			ScenePhotonView.RPC("CreateExplosion", PhotonTargets.All,PhotonNetwork.player.ID, netInst.PlayerTransform.position); 
			netInst.Die();
		}
	}
	
	[RPC]
	public void CreateExplosion(int id, Vector3 position)
	{
		Color color = RhythmMovement.PlayerColor(id);
		((GameObject) GameObject.Instantiate(Resources.Load("DieParticle"), position, Quaternion.identity)).renderer.material.color = color;
	}
	
	public void Hit (int toPlayer, float howMuch)
	{
		ScenePhotonView.RPC("NewFrag", PhotonTargets.MasterClient, PhotonNetwork.player.ID, toPlayer, howMuch);
	}
	
	void OnGUI()
	{
		/*
		string s = "";
		foreach (KeyValuePair<int, PlayerInfo> entry in localFrags)
		{
			s += "\n" + (entry.Key == PhotonNetwork.player.ID? ">" : " ") + entry.Key + ": " + entry.Value.kills + " / " + entry.Value.deaths;
		}
		
		GUI.Label(new Rect(10, 10,200,200), s);
		 */
	}

	void Update()
	{
		foreach (KeyValuePair<int, PlayerInfo> entry in localFrags)
		{
			if (entry.Key == PhotonNetwork.player.ID)
			{
				if (healthUI != null)
					healthUI.text = "" + Mathf.Floor(entry.Value.health*20);

				if (killsUI != null)
					killsUI.text = "" + entry.Value.kills;

				if (deathsUI != null)
					deathsUI.text = "" + entry.Value.deaths;

				break;
			}
		}
	}
}
