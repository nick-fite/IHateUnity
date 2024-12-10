using System;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerObject : NetworkBehaviour
{       
    // This game is completely controlled on the server side. While this has
    //issues with latency, it is a lot better for security.
    [ServerRpc]
    protected void ChangePositionServerRpc(Vector3 newPos)
    {
        ChangeLocalPosition(newPos);
    }

    [ServerRpc]
    protected void ChangeScaleServerRpc(Vector3 newScale)
    {
        ChangeLocalScale(newScale);
    }

    [ServerRpc]
    protected void ChangeRotationServerRpc(Quaternion newRot)
    {
        ChangeLocalRotation(newRot);
    }

    //these exist just for clarity. They will only change things that are
    //happening on the local version of the game.
    private void ChangeLocalPosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

    private void ChangeLocalScale(Vector3 newScale)
    {
        transform.localScale = newScale;
    }

    private void ChangeLocalRotation(Quaternion newRot)
    {
        transform.rotation = newRot;
    }

}
