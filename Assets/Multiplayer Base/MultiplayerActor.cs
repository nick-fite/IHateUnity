using UnityEngine;
using Unity.Netcode;

public class MultiplayerActor : MultiplayerObject
{
    protected CharacterController charController;

    private void Awake()
    {
        charController = GetComponent<CharacterController>();
    }

    protected void ReplicateActorMove(Vector3 moveDir, float moveSpeed)
    {
        if (IsLocalPlayer)
        {
            ReplicateActorMove(moveDir, moveSpeed);
        }
        else if (IsClient && IsLocalPlayer)
        {
            MoveActorServerRpc(moveDir, moveSpeed);

        }
    }

    [Rpc(SendTo.Server)]

    private void MoveActorServerRpc(Vector3 moveDir, float moveSpeed)
    {
       ActorMove(moveDir, moveSpeed);       
    }
    
    private void ActorMove(Vector3 moveDir, float moveSpeed)
    {
        transform.position += moveDir * moveSpeed * Time.fixedDeltaTime;
    }
}
