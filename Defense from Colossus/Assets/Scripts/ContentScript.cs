using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentScript : MonoBehaviour
{

    [SerializeField] private GameObject findLobbyListMenu;
    [SerializeField] private GameObject lobbyMenu;
    // Start is called before the first frame update
    public void UIChange(string lobbyCode)
    {
        findLobbyListMenu.SetActive(false);
        lobbyMenu.SetActive(true);
        lobbyMenu.transform.GetChild(1).GetComponent<TMPro.TMP_Text>().SetText(lobbyCode); 
    }
}
