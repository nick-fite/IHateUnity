using Unity.Netcode;
using UnityEngine;

public abstract class Widget : NetworkBehaviour
{
    protected GameObject owner;

    public virtual void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
    }
}
