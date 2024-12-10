using Unity.Netcode;
using UnityEngine;


public class HealthBar : ValueGauge
{
    public override void SetOwner(GameObject newOwner)
    {
        base.SetOwner(newOwner);
        HealthComponent ownerHealthComponent = newOwner.GetComponent<HealthComponent>();
        if (ownerHealthComponent)
        {
            ownerHealthComponent.OnHealthChanged += HealthChanged;
        }
    }

    private void HealthChanged(float newHealth, float delta, float maxHealth)
    {
        UpdateValue(newHealth, maxHealth);
    }
}
