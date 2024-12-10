using UnityEngine;

public class PlayerUI : Widget
{
    public override void SetOwner(GameObject newOwner)
    {
        base.SetOwner(newOwner);
        foreach (Transform child in transform)
        { 
            Widget widget = child.GetComponent<Widget>();
            if (widget)
            { 
                widget.SetOwner(newOwner);
            }
        }

        HealthComponent ownerHealthComponent = newOwner.GetComponent<HealthComponent>();
        if (ownerHealthComponent)
        {
            ownerHealthComponent.OnDead += OwnerNotPresent;
        }

        PlayerNetwork ownerPlayer = newOwner.GetComponent<PlayerNetwork>();
        if (ownerPlayer)
        {
            ownerPlayer.OnPlayerDisconnect += OwnerNotPresent;
        }
    }
    private void OwnerNotPresent()
    {
        Destroy(gameObject);
    }
}
