using Unity.Netcode;
using UnityEngine;

public class MultiplayerActor : NetworkBehaviour
{
    [SerializeField] protected CharacterController charController;

    private void Awake()
    {
        charController = GetComponent<CharacterController>();
    }

    protected void ReplicateActorMove(Vector3 moveDir, float moveSpeed)
    {
        if(IsServer && IsLocalPlayer)
        {   
            ActorMove(moveDir, moveSpeed);
        }
        else if(IsClient && IsLocalPlayer)
        {
            MoveActorServerRpc(moveDir, moveSpeed);
        }


    }

    [Rpc(SendTo.Server)]
    private void MoveActorServerRpc(Vector3 moveDir, float moveSpeed)
    {
        ActorMove( moveDir, moveSpeed);
    }
    private void ActorMove(Vector3 moveDir, float moveSpeed)
    {
        transform.position += moveDir * moveSpeed * Time.fixedDeltaTime;

    }
}
