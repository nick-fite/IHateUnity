using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ThrowComponent : NetworkBehaviour
{
    [SerializeField] private Transform holdingPositionTransform;
    private GameObject heldObject;

    [SerializeField] private float holdSpeed = 2f;
    private NetworkVariable<bool> _bIsHolding = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField][Range(0, 5)] private float throwVelocity = 4f;

    public float GetThrowVelocity() { return throwVelocity; }
    public void SetIsHolding(bool stateToSet) { _bIsHolding.Value = stateToSet; }
    public void SetHeldObject(GameObject heldObjToSet) 
    {
        heldObject = heldObjToSet; 
    }

    public void ClearHeldObject() 
    {
        heldObject = null; 
    }
    public bool IsHeldPosition()
    {
        if (!holdingPositionTransform)
        {
            return false;
        }
        return true;
    }
    private void FixedUpdate()
    {
        if (_bIsHolding.Value)
        {
            CheckHoldAction();
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
            HoldObject(heldObject);
        }
        else if (IsClient)
        {
            HoldObjectServerRpc(heldObject);
        }
    }

    [Rpc(SendTo.Server)]
    private void HoldObjectServerRpc(NetworkObjectReference heldObjRef)
    {
        HoldObjectClientRpc(heldObjRef);
    }
    [Rpc(SendTo.Everyone)]
    private void HoldObjectClientRpc(NetworkObjectReference heldObjRef)///<-- Need this, it doesn't get far enough without it!
    {
        HoldObject(heldObjRef);
    }
    private void HoldObject(NetworkObjectReference heldObjRef)
    {
        if (heldObject == null)
        { 
            heldObjRef.TryGet(out NetworkObject pickupNetwork);
            if (pickupNetwork != null)
            {
                Debug.Log($"Have network object though :)");
                heldObject = pickupNetwork.gameObject;
            }
        }

        if (holdingPositionTransform && heldObject)
        {
            Debug.Log($"current held obj: {heldObject.gameObject.name}, newPos: {holdingPositionTransform}, HoldSpeed: {holdSpeed}");
            Debug.Log($"slerp");
            heldObject.transform.position = holdingPositionTransform.position;
            heldObject.transform.rotation = holdingPositionTransform.rotation;
        }
    }
}
