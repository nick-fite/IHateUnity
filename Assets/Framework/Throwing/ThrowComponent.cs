using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ThrowComponent : NetworkBehaviour
{
    [SerializeField] private Transform holdingPositionTransform;
    private Transform heldObjTransform;

    [SerializeField] private float holdSpeed = 2f;
    private NetworkVariable<bool> _bIsHolding = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public void SetIsHolding(bool stateToSet) { _bIsHolding.Value = stateToSet; }
    public void SetHeldObject(Transform heldObjToSet) { heldObjTransform = heldObjToSet; }
    public void ClearHeldObject() 
    {
        heldObjTransform = null; 
    }
    public bool IsHeldPosition()
    {
        if (!holdingPositionTransform)
        {
            return false;
        }
        return true;
    }
    private void Update()
    {
        if (_bIsHolding.Value)
        {
            HoldObject();
        }
    }
    private void CheckHoldAction() 
    {
        if (!IsLocalPlayer)
        {
            return;
        }

        if (IsServer) 
        {
            HoldObject();
        }
        else if (IsClient)
        { 
            HoldObjectServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void HoldObjectServerRpc()
    {
        HoldObjectClientRpc();
    }
    [Rpc(SendTo.Everyone)]
    private void HoldObjectClientRpc()
    {
        HoldObject();
    }
    private void HoldObject()
    {
        Debug.Log($"current held obj: {heldObjTransform.gameObject.name}, newPos: {holdingPositionTransform}, HoldSpeed: {holdSpeed}");
        if (holdingPositionTransform && heldObjTransform)
        {
            Debug.Log($"slerp");
            heldObjTransform.position = Vector3.Slerp(heldObjTransform.position, holdingPositionTransform.position, holdSpeed * Time.deltaTime);
        }
    }



    //To Be Removed
    /*private void OnTriggerEnter(Collider other)
    {
        PickupComponent pickUpComponent = other.GetComponent<PickupComponent>();
        if (pickUpComponent == null)
        {
            return;
        }

        if (ShouldHoldObject(other.gameObject) && pickUpComponent.ShouldInteract(this.gameObject))
        {
            Debug.Log("Should pickup");
            _currentOverlappingTargets.Add(other.gameObject);
            heldObjTransform = other.gameObject.transform;

            PlayerNetwork player = GetComponent<PlayerNetwork>();
            if (player)
            { 
                player.SetTargetInteractible(other.gameObject);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (heldObjTransform == null || heldObjTransform.gameObject != other.gameObject || _bIsHolding.Value == true)
        {
            Debug.Log("return");
            return;
        }
        Debug.Log("removed");
        _currentOverlappingTargets.Remove(other.gameObject);

        if (_currentOverlappingTargets.Count > 0)
        {
            heldObjTransform = _currentOverlappingTargets[0].transform;
            GetComponent<PlayerNetwork>().SetTargetInteractible(heldObjTransform.gameObject);
        }
        else
        {
            heldObjTransform = null;
            GetComponent<PlayerNetwork>().SetTargetInteractible(null);
        }
    }*/
}
