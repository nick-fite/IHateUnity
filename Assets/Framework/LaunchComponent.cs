using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LaunchComponent : NetworkBehaviour
{
    Rigidbody _rigidBody;
    PlayerNetwork _playerNetwork;

    private NetworkVariable<Vector3> finalVelocity = new NetworkVariable<Vector3>();

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _playerNetwork = GetComponent<PlayerNetwork>();
    }
    public void CheckLaunch(Vector3 launchDirection, float launchVelocity) 
    {
        if (!IsLocalPlayer)
        {
            return;
        }

        finalVelocity.Value = launchDirection * launchVelocity;
        if (IsServer)
        {
            Launch(launchDirection * launchVelocity);
        }
        else if (IsClient)
        {
            LaunchServerRpc(launchDirection * launchVelocity);
        }
    }
    [Rpc(SendTo.Server)]
    private void LaunchServerRpc(Vector3 velocity) 
    {
        Launch(velocity);
    }
    private void Launch(Vector3 velocity) 
    {
        Debug.Log("launch");

        if (_rigidBody)
        {
            //_rigidBody.AddForce(finalVelocity, ForceMode.VelocityChange);
            AddRigidBodyForce(velocity);
        }
        if (_playerNetwork)//WIP
        {
            _playerNetwork.SetPlayerVelocity(velocity);
        }
    }

    private void AddRigidBodyForce(Vector3 velocity)
    {
        Debug.Log("RigidBody add force");
        _rigidBody.useGravity = true;
        _rigidBody.AddForce(velocity, ForceMode.VelocityChange);
    }
}
