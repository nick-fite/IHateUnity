using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<GameObject> positions;

    NetworkManager nm;

    private void Awake()
    {
        
        positions = new List<GameObject>();
    }

    public override void OnNetworkSpawn()
    {
        //onClientConnected(OwnerClientId);
        //NetworkManager.Singleton.OnClientConnectedCallback += onClientConnected;
    }

    private void onClientConnected(ulong clientId)
    {
        Transform spawnPos = GetRandomSpawnPos();
        GameObject player = Instantiate(_playerPrefab, spawnPos.position, spawnPos.rotation);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }

    private Transform GetRandomSpawnPos()
    {
        positions.Clear();
        positions.AddRange(GameObject.FindGameObjectsWithTag("SpawnPoints"));
        int rand = Random.Range(0, positions.Count);
        return positions[rand].transform;
    }
}
