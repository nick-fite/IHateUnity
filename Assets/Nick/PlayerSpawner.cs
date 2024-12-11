using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{

    [SerializeField] private GameObject _player;
    [SerializeField] private List<Transform> positions;


    void Start()
    {
        DontDestroyOnLoad(this.gameObject);        
    }

    public override void OnNetworkSpawn()
    {
        positions = new List<Transform>();

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
    }

    public void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log("This is being called from PlayerSpawner");
        if(IsHost)
        {
            foreach(ulong id in clientsCompleted)
            {
                Debug.Log("making player");
                int Length = positions.Count;
                int rand = Random.Range(0, Length);
                Transform pos = positions[rand];
                GameObject player = Instantiate(_player, pos.position, pos.rotation);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(id, true);
            }
        }
    }
}
