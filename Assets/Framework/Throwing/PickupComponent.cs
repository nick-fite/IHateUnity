using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PickupComponent : NetworkBehaviour, IinteractionInterface
{
    NetworkRigidbody nt;
    Rigidbody _rigidBody;
    LaunchComponent _launchComponent;
    private bool _bIsPickedUp;

    private void Start()
    {
        _bIsPickedUp = false;
        _launchComponent = GetComponent<LaunchComponent>();
        _rigidBody = GetComponent<Rigidbody>();
    }

    /*public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _bIsPickedUp.OnValueChanged += UpdatePickedUpValue;
    }*/
    public void Update()
    {
        Debug.Log($"Use gravity is: {_rigidBody.useGravity}");
    }

    private void UpdatePickedUpValue(bool prevState, bool newState)
    {
        Debug.Log($"Awake b4 statement: bIPU={prevState}, new bIPU={newState}");
    }
    /*private void ProcessPickupCheck(NetworkObjectReference playerInteractor)
    {
        if (IsLocalPlayer)
        {
            ProcessPickupServerRpc(playerInteractor);
        }
    }

    [Rpc(SendTo.Server)]
    private void ProcessPickupServerRpc(NetworkObjectReference playerInteractor)
    {
        if (playerInteractor.TryGet(out NetworkObject networkObj))
        {
            ThrowComponent throwComponent = networkObj.gameObject.GetComponent<ThrowComponent>();
            ProcessPickup(throwComponent);
        }
    }*/
    public void InteractAction(GameObject interactor)
    {
        ThrowComponent throwComponent = interactor.GetComponent<ThrowComponent>();
        if (!throwComponent || !throwComponent.TryPickUpThrowableObject())
        {
            return;
        }

        //ProcessPickupCheck(interactor);
        ProcessPickup(throwComponent);
    }
    public bool ShouldInteract(GameObject interactor)
    {
        if (interactor != gameObject)
        {
            return true;
        }

        return false;
    }
    public void ProcessPickup(ThrowComponent throwComp) 
    {
        Debug.Log($"instigator: {throwComp.gameObject}");
        Debug.Log("ProcessPickup Start");


        _bIsPickedUp = !_bIsPickedUp;
        Debug.Log("After bool changed");

        if (_bIsPickedUp == true)
        {
            PickupAction(throwComp);
        }
        else
        {
            ReleaseAction(throwComp);
            if (_launchComponent && throwComp.IsLocalPlayer)
            {
                Vector3 throwDir = throwComp.gameObject.transform.forward + throwComp.gameObject.transform.up;
                Debug.Log("launch in pickup");
                _launchComponent.LaunchServerRpc(throwDir, 4f/*, throwComp.gameObject*/);
            }
        }
    }
    public virtual void PickupAction(ThrowComponent throwComponent)
    {
        throwComponent.SetIsHolding(true);
        Debug.Log("PickupComp: PickedUp");
        //DEBUG
        if (throwComponent.IsLocalPlayer)
        {
            ToggleRigidBodyServerRpc();
        }
        //_rigidBody.useGravity = false;
    }

    public virtual void ReleaseAction(ThrowComponent throwComponent) 
    {
        throwComponent.SetIsHolding(false);
        throwComponent.ClearHeldObj();
        Debug.Log("PickupComp: released");
        if (throwComponent.IsLocalPlayer)
        { 
            ToggleRigidBodyServerRpc();
        }
        //_rigidBody.useGravity = true;
    }

    [Rpc(SendTo.Server)]
    public void ToggleRigidBodyServerRpc()
    {
        ToggleRigidBodyClientRpc();
    }
    [Rpc(SendTo.Everyone)]
    public void ToggleRigidBodyClientRpc() 
    {
        ToggleRigidBody();
    }
    private void ToggleRigidBody()
    {
        Debug.Log("Toggle Rigidbody gravity");
        //DEBUG
        _rigidBody.useGravity = !_rigidBody.useGravity;
    }
}
