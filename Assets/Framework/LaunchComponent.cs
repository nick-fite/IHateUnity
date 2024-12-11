using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LaunchComponent : NetworkBehaviour
{
    Rigidbody _rigidBody;
    PlayerNetwork _playerNetwork;

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _playerNetwork = GetComponent<PlayerNetwork>();
    }
    public void CheckLaunch(Vector3 launchDirection, float launchVelocity, ThrowComponent thrower) 
    {
        if (!thrower.IsLocalPlayer)
        {
            return;
        }
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
    public void LaunchServerRpc(Vector3 finalVelocity) 
    {
        LaunchClientRpc(finalVelocity);
    }
    [Rpc(SendTo.Everyone)]
    private void LaunchClientRpc(Vector3 finalVelocity)
    {
        Launch(finalVelocity);
    }
    private void Launch(Vector3 velocity) 
    {
        Debug.Log("launch");

        if (_rigidBody)
        {
            AddRigidBodyForce(velocity);
        }
        if (_playerNetwork)//WIP (Remove this statement and the function inside completely if unused)
        {
            //_playerNetwork.SetPlayerVelocity(velocity);
        }
    }

    private void AddRigidBodyForce(Vector3 velocity)
    {
        Debug.Log("RigidBody add force");
        _rigidBody.useGravity = true;
        _rigidBody.AddForce(velocity, ForceMode.VelocityChange);
    }
}
