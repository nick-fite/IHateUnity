using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BombFunctionality : DamageComponent
{
    [SerializeField] private float damage = 100; 
    [SerializeField] private float explosionRadius = 5f;  
    [SerializeField] private float countdownTime = 2f;  
    private bool isExploding = false;

    public override void DoDamage()
    {
        StartCoroutine(BombCountdown());
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!isExploding && other.GetComponentInParent<HealthComponent>() != null)
        {

            Debug.LogWarning($"{other.gameObject.name} touched the bomb, starting countdown!");
            StartCoroutine(BombCountdown());
        }
    }

    private IEnumerator BombCountdown()
    {
        isExploding = true;  
        yield return new WaitForSeconds(countdownTime);
        if(IsClient && IsLocalPlayer)
        {
            ExplodeServerRpc();
        } 
        else if(IsServer && IsLocalPlayer)
        {

            Explode();  
        }
    }

    [Rpc(SendTo.Server)]
    private void ExplodeServerRpc()
    {
        Explode();
    }

    private void Explode()
    {
       
        Debug.Log("Bomb exploded!");

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            GameObject target = hitCollider.gameObject;
            Debug.LogWarning(target.name);
            HealthComponent healthComponent = target.GetComponentInParent<HealthComponent>();

            if (healthComponent != null)
            {
                Debug.Log($"Applying {damage} damage to {target.name}");
                ApplyDamageToHealth(target, damage);
            }
        }

        Destroy(gameObject);
    }

    private void ApplyDamageToHealth(GameObject target, float damageAmount)
    {
        HealthComponent healthComponent = target.transform.parent.GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.ChangeHealth(-damageAmount);  
        }
        else
        {
            Debug.LogWarning($"No HealthComponent found on {target.name}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);  
    }

 
}