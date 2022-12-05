using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // Singleton
    [SerializeField] private GameObject nameText;
    [SerializeField] private GameObject lobbyText;
    [SerializeField] private GameObject leaveButton;
    [SerializeField] private GameObject joinCodeInputField;

    [SerializeField] private GameObject lobbyNameInputField;
    [SerializeField] private GameObject mapDropdownText;
    [SerializeField] private GameObject gameModeDropdown;
    [SerializeField] private GameObject lobbySizeDropdown;
    [SerializeField] private GameObject lobbyPrivateToogle;
    [SerializeField] private GameObject startGameButton;

    [SerializeField] private GameObject editLobbyNameInputField;
    [SerializeField] private GameObject editMapDropdownText;
    [SerializeField] private GameObject editGameModeDropdown;
    [SerializeField] private GameObject editLobbySizeDropdown;
    [SerializeField] private GameObject editLobbyPrivateToogle;

    [SerializeField] private GameObject findLobbyListMenu;
    [SerializeField] private GameObject lobbyMenu;
    

    [SerializeField] private GameObject lobbyItem;
    [SerializeField] private GameObject lobbyListContent;


    private string _lobbyCode;
    private string _lobbyId;


    private RelayHostData _hostData;
    private RelayJoinData _joinData;


    async void Start()
    {
        // Initialize unity services
        await UnityServices.InitializeAsync();

        // Setup events listeners
        SetupEvents();

        // Unity Login
        await SignInAnonymouslyAsync();


    }

    #region UnityLogin

    void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () => {
            // Shows how to get a playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

            // Shows how to get an access token
            Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken}");
        };

        AuthenticationService.Instance.SignInFailed += (err) => {
            Debug.LogError(err);
        };

        AuthenticationService.Instance.SignedOut += () => {
            Debug.Log("Player signed out.");
        };
    }

    async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");
        }
        catch (Exception ex)
        {
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    #endregion

    #region Lobby

    public async void CreateLobby()
    {
        Debug.Log("Creating a new lobby...");

        try
        {
            string lobbyName = lobbyNameInputField.GetComponent<TMPro.TMP_InputField>().text;

            int maxPlayers = int.Parse(lobbySizeDropdown.GetComponent<TMPro.TMP_Text>().text);

            string map = mapDropdownText.GetComponent<TMPro.TMP_Text>().text;


            // Create the lobby
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
            _lobbyCode = lobby.LobbyCode;
            _lobbyId = lobby.Id;

            //Insert Lobby Code in lobby and Update lobby with options
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions();
            updateOptions.HostId = AuthenticationService.Instance.PlayerId;
            updateOptions.Data = new Dictionary<string, DataObject>()
            {
                //{"joinCode", new DataObject(DataObject.VisibilityOptions.Public, _hostData.JoinCode)},
                {"Map", new DataObject(DataObject.VisibilityOptions.Public, map) },
                {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameModeDropdown.GetComponent<TMPro.TMP_Text>().text)},
                {"PlayerName", new DataObject(DataObject.VisibilityOptions.Public, nameText.GetComponent<TMPro.TMP_InputField>().text) },
                {"LobbyCode", new DataObject(DataObject.VisibilityOptions.Public, _lobbyCode)},
            };
            var updateLobby = await LobbyService.Instance.UpdateLobbyAsync(_lobbyId, updateOptions);

            //Show Code
            UpdateLobbyCode();

            // Heartbeat the lobby every 15 seconds.
            StartCoroutine(HeartbeatLobbyCoroutine(_lobbyId, 15));
            StartCoroutine(LobbyPoll(_lobbyId, 1.1f));

        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyid, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyid);
            Debug.Log("Lobby Heartbit");
            yield return delay;
        }
    }

    IEnumerator LobbyPoll(string lobbyid, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        yield return delay;
    }

    public async void JoinLobby()
    {
        Debug.Log("Looking for a lobby...");

        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions{};
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCodeInputField.GetComponent<TMPro.TMP_InputField>().text, options);

            UpdateLobbyCode();
            
            Debug.Log("Joined lobby: " + lobby.Id);
            Debug.Log("Lobby Players: " + lobby.Players.Count);
            /*

            // Retrieve the Relay code previously set in the create match
            string joinCode = lobby.Data["joinCode"].Value;

            Debug.Log("Received code: " + joinCode);

            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            // Create Object
            _joinData = new RelayJoinData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                HostConnectionData = allocation.HostConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            // Set transport data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                _joinData.IPv4Address,
                _joinData.Port,
                _joinData.AllocationIDBytes,
                _joinData.Key,
                _joinData.ConnectionData,
                _joinData.HostConnectionData);

            // Finally start the client
            NetworkManager.Singleton.StartClient();
            */

        }
        catch (LobbyServiceException e)
        {
            // If we don't find any lobby, let's create a new one
            Debug.Log("Cannot find a lobby: " + e);
        }

    }
    
    public async void FindLobby()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                GameObject GO = lobbyItem;
                GO.transform.GetChild(0).GetComponent<TMPro.TMP_Text>().text = lobby.Name;
                GO.transform.GetChild(1).GetComponent<TMPro.TMP_Text>().text = lobby.Data["GameMode"].Value;
                GO.transform.GetChild(2).GetComponent<TMPro.TMP_Text>().text = lobby.Data["PlayerName"].Value;
                GO.transform.GetChild(3).GetComponent<TMPro.TMP_Text>().text = (lobby.MaxPlayers - lobby.AvailableSlots) + "/" + lobby.MaxPlayers;
                GO.transform.GetChild(4).GetComponent<TMPro.TMP_Text>().text = lobby.Data["LobbyCode"].Value;
                
                if (lobby.AvailableSlots != 0)
                {
                    Instantiate(GO).transform.SetParent(lobbyListContent.transform);

                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public void ClearFindLobby()
    {
        foreach(Transform child in lobbyListContent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public async void JoinFoundLobby(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions { };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);

            UpdateLobbyCode();
            Debug.Log("Joined lobby: " + lobby.Id);
            Debug.Log("Lobby Players: " + lobby.Players.Count);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log("Exception: "+ e);
        }
        
        
    }

    public async void JoinRandomLobby()
    {
        Debug.Log("Looking for a lobby...");

        try
        {

            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

            UpdateLobbyCode();

            findLobbyListMenu.SetActive(false);
            lobbyMenu.SetActive(true); 

            /*
            // Retrieve the Relay code previously set in the create match
            string joinCode = lobby.Data["joinCode"].Value;

            Debug.Log("Received code: " + joinCode);

            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            // Create Object
            _joinData = new RelayJoinData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                HostConnectionData = allocation.HostConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            // Set transport data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                _joinData.IPv4Address,
                _joinData.Port,
                _joinData.AllocationIDBytes,
                _joinData.Key,
                _joinData.ConnectionData,
                _joinData.HostConnectionData);

            // Finally start the client
            NetworkManager.Singleton.StartClient();

            */
        }
        catch (LobbyServiceException e)
        {
            // If we don't find any lobby, pop up Exception
            Debug.Log("Cannot find a lobby: " + e);

        }
    }

    public void UpdateLobbyCode()
    {
        lobbyText.GetComponent<TMPro.TMP_Text>().SetText(_lobbyCode);
    }

    public void OnDestroy()
    {
        // We need to delete the lobby when we're not using it
        Lobbies.Instance.DeleteLobbyAsync(_lobbyId);
        
    }


    public async void UpdateLobby(string lobbyId, string lobbyCode)
    {
        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions();
            options.Name = editLobbyNameInputField.GetComponent<TMPro.TMP_InputField>().text;
            options.MaxPlayers = int.Parse(editLobbySizeDropdown.GetComponent<TMPro.TMP_Text>().text);
            options.IsPrivate = editLobbyPrivateToogle.GetComponent<Toggle>().isOn;

            //Ensure you sign-in before calling Authentication Instance
            //See IAuthenticationService interface
            options.HostId = AuthenticationService.Instance.PlayerId;

            options.Data = new Dictionary<string, DataObject>()
            {
               {"PlayerName", new DataObject(DataObject.VisibilityOptions.Public, nameText.GetComponent<TMPro.TMP_InputField>().text) },
                {"LobbyCode", new DataObject(DataObject.VisibilityOptions.Public, lobbyCode) },
                //{"joinCode", new DataObject(DataObject.VisibilityOptions.Public, _hostData.JoinCode)},
                {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameModeDropdown.GetComponent<TMPro.TMP_Text>().text)}
            };

            var lobby = await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }



    #endregion


    public async void StartGame()
    {
        int maxConnections = 1;
        try
        {
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);
            _hostData = new RelayHostData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            // Retrieve JoinCode
            _hostData.JoinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);


            Debug.Log(_hostData.JoinCode);

            // Now that RELAY and LOBBY are set...

            // Set Transports data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                _hostData.IPv4Address,
                _hostData.Port,
                _hostData.AllocationIDBytes,
                _hostData.Key,
                _hostData.ConnectionData);

            // Finally start host
            NetworkManager.Singleton.StartHost();

        }
        catch(Exception e)
        {
            Debug.Log("Exception: " + e);
        }

    }

    #region Relay
    public struct RelayHostData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] Key;
    }
    

    public struct RelayJoinData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] HostConnectionData;
        public byte[] Key;
    }
    #endregion Relay


}