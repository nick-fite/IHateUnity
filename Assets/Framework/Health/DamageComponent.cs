using System;
using Unity.Netcode;
using UnityEngine;


public abstract class DamageComponent : NetworkBehaviour
{
   [SerializeField] private bool bAttackFriendly;
   [SerializeField] private bool bAttackEnemy;
   [SerializeField] private bool bAttackNeutral;

   private ITeamInterface _teamInterface;

   public abstract void DoDamage();
   private void Awake()
   {
      _teamInterface = GetComponent<ITeamInterface>();
   }

   protected void ApplyDamage(GameObject target, float damageAmt)
   {
      Debug.Log("applying damage");
      HealthComponent targetHealthComponent = target.GetComponentInParent<HealthComponent>();
      if (targetHealthComponent != GetComponent<HealthComponent>() && targetHealthComponent != null)
      {
         Rigidbody rb = target.GetComponent<Rigidbody>();
         if(rb != null)
         {
            rb.AddExplosionForce(1, transform.position, 3);
         }
         targetHealthComponent.ChangeHealth(-damageAmt);
      }
   }

   public bool ShouldDamage(GameObject target)
   {
      TeamAttitude teamAttitude = _teamInterface.GetTeamAttitudeTowards(target);

      if (teamAttitude == TeamAttitude.Enemy && bAttackEnemy)
         return true;
      if (teamAttitude == TeamAttitude.Friendly && bAttackFriendly)
         return true;
      if (teamAttitude == TeamAttitude.Neutral && bAttackNeutral)
         return true;
      Debug.Log("Cannot attack");
      return false;
   }
}
