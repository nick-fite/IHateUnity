using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class PlayerNetworkMovement : MultiplayerActor
{
    bool _bCanMove;
    [SerializeField] private CinemachineCamera _VCam;
    [SerializeField] private Camera _cam;
    [SerializeField] float _turnSmoothVelocity;
    [SerializeField] float _turnSmoothTime;
    [SerializeField] Animator _animator;
    [SerializeField] float _gravity;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _bCanMove = true;
    }

    public void MovePlayer(Vector2 rawInput, float moveSpeed)
    {
        float targetAngle = GetTargetAngle(rawInput);

        float rotAngle = GetRotationAngle(targetAngle);
        
        if(IsServer && IsLocalPlayer)
        {
            if(rawInput != Vector2.zero){
                Move(targetAngle, rotAngle, moveSpeed);
                ControlWalkingAnimation(true);
            }
            else
            {
                ControlWalkingAnimation(false);
            }
            ApplyGravity();
        }
        else if(IsClient && IsLocalPlayer)
        {
            if(rawInput != Vector2.zero)
            {
                MovePlayerServerRpc(targetAngle, rotAngle, moveSpeed);
                ControlWalkingAnimationServerRpc(true);
            }
            else 
            {
                ControlWalkingAnimationServerRpc(false);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerServerRpc(float targetAngle, float rotationAngle, float moveSpeed)
    {
        Move(targetAngle, rotationAngle, moveSpeed);
        ApplyGravity();
    }

    private void Move(float targetAngle, float rotationAngle, float moveSpeed)
    {
        Quaternion newAngle = Quaternion.Euler(0f, rotationAngle, 0f);
        transform.rotation = newAngle;

        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        
        charController.Move(moveDir.normalized * (moveSpeed * Time.fixedDeltaTime));
        ApplyGravity();
    }

    private float GetTargetAngle(Vector2 rawInput)
    {
        float targetAngle = 0.0f;
        //Moving character
        Vector3 movementValue = new Vector3(rawInput.x, 0, rawInput.y).normalized;
        targetAngle = Mathf.Atan2(movementValue.x, movementValue.z) * Mathf.Rad2Deg + _cam.transform.eulerAngles.y;

        return targetAngle;
    }

    private float GetRotationAngle(float targetAngle)
    {
        return Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
    }

    private void ApplyGravity()
    {
        if(charController.isGrounded)
        {
            return;
        }

        charController.Move(Physics.gravity * Time.fixedDeltaTime);
    }

    [Rpc(SendTo.Server)]
    private void ControlWalkingAnimationServerRpc(bool ContinueAnim)
    {
        ControlWalkingAnimation(ContinueAnim);
    }

    private void ControlWalkingAnimation(bool ContinueAnim)
    {
        _animator.SetBool("walking", ContinueAnim);
    }

}
