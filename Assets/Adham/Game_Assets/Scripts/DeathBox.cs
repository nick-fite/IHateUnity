using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class DeathBox : DamageComponent
{
    [SerializeField] private float damage = 100;
    private HashSet<GameObject> _currentOverlappingTargets = new HashSet<GameObject>();
    private bool _bCanDamage = true;

    public override void DoDamage()
    {
        ApplyAllDamage();
    }


    private void ApplyAllDamage()
    {
        Debug.Log("Do the damage please");
        if (_bCanDamage == false)
        {
            return;
        }

        foreach (GameObject target in _currentOverlappingTargets)
        {
            Debug.Log($"Applying damage to: {target.name}");
            ApplyDamageToHealth(target, damage);
        }

        _bCanDamage = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{other.gameObject.name} entered the DeathBox");

        HealthComponent healthComponent = other.transform.parent.GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            Debug.Log("Valid target found with HealthComponent");
            _currentOverlappingTargets.Add(other.transform.parent.gameObject);
            ApplyDamageToHealth(other.transform.parent.gameObject, damage);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _currentOverlappingTargets.Remove(other.gameObject);
    }

    private void ApplyDamageToHealth(GameObject target, float damageAmount)
    {
        HealthComponent healthComponent = target.GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.ChangeHealth(-damageAmount);
        }
        else
        {
            Debug.LogWarning($"No HealthComponent found on {target.name}");
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _bCanDamage);
    }
}
