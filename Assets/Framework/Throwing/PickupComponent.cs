using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Rigidbody))]
public class PickupComponent : NetworkBehaviour, IinteractionInterface
{
    PlayerNetwork _playerNetwork;
    CharacterController _characterController;
    Rigidbody _rigidBody;
    LaunchComponent _launchComponent;
    private bool _bIsPickedUp;
    private Collider _collider;

    private void Start()
    {
        _bIsPickedUp = false;
        _playerNetwork = GetComponent<PlayerNetwork>();
        _characterController = GetComponent<CharacterController>();
        _launchComponent = GetComponent<LaunchComponent>();
        _rigidBody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }
    public bool ShouldInteract(GameObject interactor)
    {
        ThrowComponent throwComponent = interactor.GetComponent<ThrowComponent>();
        if (!throwComponent || !throwComponent.IsHeldPosition())
        {
            return false;
        }
        if (interactor == this.gameObject)
        {
            return false;
        }
        return true;
    }
    public void InteractAction(GameObject interactor)
    {
        ThrowComponent throwComponent = interactor.GetComponent<ThrowComponent>();
        ProcessPickup(throwComponent);
    }
    private void ProcessPickup(ThrowComponent throwComp) 
    {
        _bIsPickedUp = !_bIsPickedUp;
        Debug.Log($"Toggle _bIsPickedUp: {_bIsPickedUp}");

        if (_bIsPickedUp == true)
        {
            PickupAction(throwComp);
        }
        else
        {
            ReleaseAction(throwComp);
        }
    }

    protected virtual void PickupAction(ThrowComponent throwComp)
    {
        Debug.Log("PickupComp: PickedUp");
        if (!throwComp.IsLocalPlayer)
        {
            return;
        }
        throwComp.SetHeldObject(this.gameObject);
        ToggleRigidBodyServerRpc();
        throwComp.SetIsHolding(true);
    }

    protected virtual void ReleaseAction(ThrowComponent throwComp) 
    {
        Debug.Log("PickupComp: released");
        if (!throwComp.IsLocalPlayer)
        {
            return;
        }
        throwComp.SetIsHolding(false);
        throwComp.ClearHeldObject();
        ToggleRigidBodyServerRpc();
        if (_launchComponent && throwComp.IsLocalPlayer)
        {
            _rigidBody.isKinematic = false;
            _collider.isTrigger = false;
            PlayerNetwork player = throwComp.GetComponent<PlayerNetwork>();

            Vector3 throwDir = player.GetPlayerForward() + player.GetPlayerUp();
            Debug.Log("launch in pickup");
            _launchComponent.CheckLaunch(throwDir, throwComp.GetThrowVelocity(), throwComp);
        }
    }

    [Rpc(SendTo.Server)]
    private void ToggleRigidBodyServerRpc()
    {
        ToggleRigidBody();
    }
    private void ToggleRigidBody()
    {
        _rigidBody.useGravity = !_rigidBody.useGravity;
        Debug.Log($"Toggle Rigidbody gravity: {_rigidBody.useGravity}");
    }
}
