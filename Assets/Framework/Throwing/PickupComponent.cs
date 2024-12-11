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
    private Collider _col;

    private void Start()
    {
        _bIsPickedUp = false;
        _playerNetwork = GetComponent<PlayerNetwork>();
        _characterController = GetComponent<CharacterController>();
        _launchComponent = GetComponent<LaunchComponent>();
        _rigidBody = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
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

            if (_launchComponent && throwComp.IsLocalPlayer)
            {
                _rigidBody.isKinematic = false;
                _col.isTrigger = false;
                Vector3 throwDir = throwComp.gameObject.transform.forward + throwComp.gameObject.transform.up;
                Debug.Log("launch in pickup");
                _launchComponent.LaunchServerRpc(throwDir, 4f/*, throwComp.gameObject*/);
            }
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
        SetPickUpPlayerMoveability(false);
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
        SetPickUpPlayerMoveability(true);
        if (_launchComponent)
        {
            PlayerNetwork player = throwComp.GetComponent<PlayerNetwork>();
            Vector3 throwDir = player.GetPlayerUp() + player.GetPlayerForward();
            Debug.Log("launch in pickup");
            _launchComponent.CheckLaunch(throwDir, throwComp.GetThrowVelocity(), throwComp);
        }
    }

    private void SetPickUpPlayerMoveability(bool stateToSet) //WIP
    {
        if (_characterController)
        {
            _characterController.enabled = stateToSet;
        }
        if (_playerNetwork)
        {
            _playerNetwork.enabled = stateToSet;
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
