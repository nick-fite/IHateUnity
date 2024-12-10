using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BoxTriggerDamageComponent : DamageComponent
{
    [SerializeField] private float damage = 10;
    private HashSet<GameObject> _currentOverlappingTargets = new HashSet<GameObject>();

    [SerializeField] private float damageCooldownTime = 1f;
    private bool _bCanDamage = true;
    
    public override void DoDamage()
    {
        if (IsServer && IsLocalPlayer)
        {
            DoDamageClientRpc();
        }
        else if (IsClient && IsLocalPlayer)
        {
            DoDamageClientRpc();
        }
    }
    [Rpc(SendTo.Server)]
    private void DoDamageServerRpc() 
    {
        //ApplyAllDamage();
        DoDamageClientRpc();
    }
    [Rpc(SendTo.Everyone)]
    private void DoDamageClientRpc()
    {
        ApplyAllDamage();
    }
    private void ApplyAllDamage()
    {
        Debug.Log("Do damage");
        if (_bCanDamage == false)
        {
            return;
        }

        _bCanDamage = false;
        foreach (GameObject target in _currentOverlappingTargets)
        {
            Debug.Log($"Doing damage/apply: {target.name}");
            ApplyDamage(target, damage);
        }
        BeginCooldown();
    }

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        if (ShouldDamage(other.gameObject))
        {
            Debug.Log("Should damage/ontrigger enter");
            _currentOverlappingTargets.Add(other.gameObject);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        _currentOverlappingTargets.Remove(other.gameObject);
    }

    private void BeginCooldown() 
    {
        if (IsServer && IsLocalPlayer)
        {
            BeginCooldownServerRpc();
        }
        else if (IsClient && IsLocalPlayer)
        { 
            BeginCooldownClientRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void BeginCooldownServerRpc()
    {
        BeginCooldownClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void BeginCooldownClientRpc()
    {
        StartCoroutine(DamageCooldown(damageCooldownTime));
    }

    private IEnumerator DamageCooldown(float waitTime) 
    {
        yield return new WaitForSeconds(waitTime);
        _bCanDamage = true;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _bCanDamage);
        serializer.SerializeValue(ref damageCooldownTime);
    }
}
