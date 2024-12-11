using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ThrowComponent : NetworkBehaviour
{
    [SerializeField] private Transform holdTransform;
    [SerializeField]private Transform heldObjTransform;

    [SerializeField] private float holdSpeed = 2f;
    private NetworkVariable<bool> _bIsHolding = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private List<GameObject> _currentOverlappingTargets = new List<GameObject>();

    public void SetIsHolding(bool stateToSet) { _bIsHolding.Value = stateToSet; }
    public void ClearHeldObj() 
    {
        _currentOverlappingTargets.Remove(heldObjTransform.gameObject);
        heldObjTransform = null; 
    }

    private void Update()
    {
        if (_bIsHolding.Value)
        {
            HoldObject();
        }
    }

    private void HoldObject()
    {
        if (holdTransform && heldObjTransform)
        {
            heldObjTransform.position = Vector3.Slerp(heldObjTransform.position, holdTransform.position, holdSpeed * Time.deltaTime);
        }
    }
    private bool ShouldHoldObject(GameObject other) 
    {
        return !_currentOverlappingTargets.Contains(other.gameObject);
    }
    public bool TryPickUpThrowableObject()
    {
        if (!heldObjTransform)
        {
            return false;
        }
        return true;
    }
    public void OnTriggerEnter(Collider other)
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
            return;//held object is still in range.
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
    }
}
