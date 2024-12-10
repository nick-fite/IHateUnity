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

    private void Start()
    {
        _bIsPickedUp = false;
        _playerNetwork = GetComponent<PlayerNetwork>();
        _characterController = GetComponent<CharacterController>();
        _launchComponent = GetComponent<LaunchComponent>();
        _rigidBody = GetComponent<Rigidbody>();
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
        throwComponent.SetHeldObject(this.gameObject.transform);
        ProcessPickup(throwComponent);
    }
    private void ProcessPickup(ThrowComponent throwComp) 
    {
        Debug.Log($"instigator: {throwComp.gameObject}");

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
    /*[Rpc(SendTo.Server)]
    private void TogglePickedUpServerRpc() 
    {
        TogglePickedUp();
    }

    private void TogglePickedUp()
    {
        _bIsPickedUp = !_bIsPickedUp;
    }*/

    protected virtual void PickupAction(ThrowComponent throwComp)
    {
        throwComp.SetIsHolding(true);
        Debug.Log("PickupComp: PickedUp");
        if (throwComp.IsLocalPlayer)
        {
            ToggleRigidBodyServerRpc();
        }
        SetPickUpPlayerMoveability(false);
    }

    protected virtual void ReleaseAction(ThrowComponent throwComp) 
    {
        throwComp.SetIsHolding(false);
        throwComp.ClearHeldObject();
        Debug.Log("PickupComp: released");
        if (!throwComp.IsLocalPlayer)
        {
            return;
        }
        ToggleRigidBodyServerRpc();
        SetPickUpPlayerMoveability(true);
        if (_launchComponent)
        {
            Vector3 throwDir = throwComp.gameObject.transform.forward + throwComp.gameObject.transform.up;
            Debug.Log("launch in pickup");
            _launchComponent.CheckLaunch(throwDir, 4f/*, throwComp.gameObject*/);
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
        ToggleRigidBodyClientRpc();
    }
    [Rpc(SendTo.Everyone)]
    private void ToggleRigidBodyClientRpc() 
    {
        ToggleRigidBody();
    }
    private void ToggleRigidBody()
    {
        _rigidBody.useGravity = !_rigidBody.useGravity;
        Debug.Log($"Toggle Rigidbody gravity: {_rigidBody.useGravity}");
    }
}
