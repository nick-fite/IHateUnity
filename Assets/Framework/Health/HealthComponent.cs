using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class HealthComponent : NetworkBehaviour
{
    public delegate void OnHealthChangedDelegate(float newHealth, float delta, float maxHealth);
    public delegate void OnDeadDelegate();
    public event OnHealthChangedDelegate OnHealthChanged;
    public event OnHealthChangedDelegate OnTakenDamage;
    public event OnDeadDelegate OnDead;
    
    [SerializeField] private float maxHealth = 100;
    private NetworkVariable<float> _health = new NetworkVariable<float>();
   
    private void Awake()
    {
        Debug.Log($"Awake b4 statement: h={_health.Value}, mH={maxHealth}");
       _health.Value = maxHealth;
        Debug.Log($"Awake aftr statement: h={_health.Value}, mH={maxHealth}");
    }
    public override void OnNetworkSpawn() 
    {
        _health.OnValueChanged += UpdateHealthValue;
    }
    public void RefreshHealth() 
    {
        if (IsLocalPlayer)
        {
            RefreshHealthServerRpc(_health.Value);
        }
    }

    [Rpc(SendTo.Server)]
    private void RefreshHealthServerRpc(float health)
    {
        RefreshHealthClientRpc(health);
    }

    [Rpc(SendTo.Everyone)]
    private void RefreshHealthClientRpc(float health)
    {
        RefreshCurrentHealth(health);
    }

    private void RefreshCurrentHealth(float health)
    {
        OnHealthChanged?.Invoke(health, 0, maxHealth);
    }

    public void ChangeHealth(float amt)
    {
        if (_health.Value == 0)
            return;
        Debug.Log("Before clamp");
        _health.Value = Mathf.Clamp(_health.Value + amt, 0, maxHealth);
        Debug.Log("After clamp");
        if (amt < 0)
        {
            OnTakenDamage?.Invoke(_health.Value, amt, maxHealth);//not used right now
        }
        OnHealthChanged?.Invoke(_health.Value, amt, maxHealth);

        if (_health.Value <= 0)
        {
            OnDead?.Invoke();
        }
    }
    private void UpdateHealthValue(float prevAmount, float currentAmount)
    {
        Debug.Log($"Update Health: prevAmt={prevAmount}, currAmt={currentAmount}");
        OnHealthChanged?.Invoke(currentAmount, 0, maxHealth);
        if (currentAmount <= 0)
        {
            OnDead?.Invoke();
        }
    }
}
