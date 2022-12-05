using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyItemScript:MonoBehaviour
{
	[SerializeField] private Button yourButton;
	[SerializeField] private GameObject lobbyCode;
	GameManager gameManager;
	
	void Start()
	{
		Button btn = yourButton.GetComponent<Button>();
		btn.onClick.AddListener(TaskOnClick);
		
	}

	public async void TaskOnClick()
	{
        try
        {
			string code = lobbyCode.GetComponent<TMPro.TMP_Text>().text;
			JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions { };
			Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);

			this.transform.GetComponentInParent<ContentScript>().UIChange(code);
		}
        catch(Exception e)
        {
			Debug.Log("Exception" + e);
        }
		
	}
}
