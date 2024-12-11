using System;
using Unity.Netcode;
using UnityEngine;

public class LaunchComponent : MonoBehaviour
{
    Rigidbody _rigidBody;
    PlayerNetwork _playerNetwork;

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _playerNetwork = GetComponent<PlayerNetwork>();
    }
    [Rpc(SendTo.Server)]
    public void LaunchServerRpc(Vector3 launchDirection, float launchVelocity) 
    {
        LaunchClientRpc(launchDirection, launchVelocity);
    }

    [Rpc(SendTo.Everyone)]
    public void LaunchClientRpc(Vector3 launchDirection, float launchVelocity) 
    {
        Launch(launchDirection, launchVelocity);
    }

    public void Launch(Vector3 launchDirection, float launchVelocity/*, GameObject instigator = null*/) 
    {
        Debug.Log("launch");

        /*if (instigator && !instigator.GetComponent<NetworkBehaviour>().IsLocalPlayer)
        {
            return;
        }*/
        Vector3 finalVelocity = launchDirection * launchVelocity;

        if (_rigidBody)
        {
            //_rigidBody.AddForce(finalVelocity, ForceMode.VelocityChange);
            AddRigidBodyForceServerRpc(finalVelocity, ForceMode.VelocityChange);
        }
        if (_playerNetwork)
        {
            _playerNetwork.SetPlayerVelocity(finalVelocity);
        }
    }

    [Rpc(SendTo.Server)]
    public void AddRigidBodyForceServerRpc(Vector3 velocity, ForceMode mode)
    {
        AddRigidBodyForceClientRpc(velocity, mode);
    }
    [Rpc(SendTo.Everyone)]
    public void AddRigidBodyForceClientRpc(Vector3 velocity, ForceMode mode)
    {
        AddRigidBodyForce(velocity, mode);
    }
    private void AddRigidBodyForce(Vector3 velocity, ForceMode mode)
    {
        Debug.Log($"RigidBodyForce - Velocity: {velocity.x}, {velocity.y}, {velocity.z} - Mode: {mode}");

        _rigidBody.AddForce(velocity, mode);
    }
}
