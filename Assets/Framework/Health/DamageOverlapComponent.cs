using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DamageOverlapComponent : DamageComponent
{
    [Header("Attack Options")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] private float damage = 10;
    [SerializeField] private float damageCooldownTime = 1f;
    private bool _bCanDamage = true;
    public override void DoDamage()
    {
        DoDamageClientRpc();
    }
    [Rpc(SendTo.Server)]
    private void DoDamageServerRpc()
    {
        DoDamageClientRpc();
    }
    [Rpc(SendTo.Everyone)]
    private void DoDamageClientRpc()
    {
        ApplyAllDamage();
    }
    private void ApplyAllDamage()
    {
        if (_bCanDamage == false)
        {
            return;
        }
        Debug.Log("Do damage");

        _bCanDamage = false;
        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin.position, attackRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            if (ShouldDamage(hitCollider.gameObject))
            {
                ApplyDamage(hitCollider.gameObject, damage);
            }
        }
        BeginCooldown();
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
    private void OnDrawGizmos()
    {
        if (_bCanDamage)
        {
            return;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }
}
