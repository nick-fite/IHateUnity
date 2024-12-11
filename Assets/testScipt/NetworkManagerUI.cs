using NUnit.Framework.Internal.Filters;
using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : NetworkBehaviour, INetworkSerializable
{
    [SerializeField] private GameObject menu;
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private GameObject ipBox;

    string _playerName = "Player Name";
    string _ip = "127.0.0.1";
    private void Awake()
    {
        serverBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
            serverBtn.gameObject.SetActive(false);
        });
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            hostBtn.gameObject.SetActive(false);
        });
        clientBtn.onClick.AddListener(() => {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(_ip,7777);
            NetworkManager.Singleton.StartClient();

            clientBtn.gameObject.SetActive(false);
        });

        NetworkManager.OnClientStarted += SetCurrentPlayerName;
    }

       

    public void SetCurrentPlayerName()
    {
        Debug.Log($"PlayerName: {_playerName}");
        NameChanged(_playerName);
        HideMenu();
    }
    private void HideMenu() 
    {
        if (menu)
        { 
            menu.SetActive(false);
        }
    }

    public void IPChanged(string ip)
    {
        _ip = ip;

    }

    public void NameChanged(string newName)
    {
        _playerName = newName;
        if (!NetworkManager.Singleton.LocalClient.PlayerObject)
        {
            return;
        }
        GameObject playerObj = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        if (!playerObj)
        {
            return;
        }
        NameComponent playerNameComponent = playerObj.GetComponent<NameComponent>();
        if (playerNameComponent)
        {
            playerNameComponent.OnNameChanged?.Invoke(newName);
        }
    }
    public string GetPlayerName() 
    {
        return _playerName;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _playerName);
    }
}
