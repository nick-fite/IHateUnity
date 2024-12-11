using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour, INetworkSerializable
{
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    string ip;
    ushort port;
    public NetworkDriver driver;

    string _playerName;
    private void Awake()
    {
        serverBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
        });
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
        });
        clientBtn.onClick.AddListener(() => {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(ip, 7777);
            NetworkManager.Singleton.StartClient();
        });
    }

    public void PortChanged(string _port)
    {
        //port = (ushort)_port;
    }

    public void IPChanged(string _ip)
    {
        ip = _ip;

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
