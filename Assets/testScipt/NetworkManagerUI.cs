using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour, INetworkSerializable
{
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;

    string _playerName;
    private void Awake()
    {
        serverBtn.onClick.AddListener(() => {
            this.gameObject.SetActive(false);
            NetworkManager.Singleton.StartServer();
        });
        hostBtn.onClick.AddListener(() => {
            this.gameObject.SetActive(false);
            NetworkManager.Singleton.StartHost();
        });
        clientBtn.onClick.AddListener(() => {
            this.gameObject.SetActive(false);
            NetworkManager.Singleton.StartClient();
        });
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
