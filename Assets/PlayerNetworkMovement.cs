using System;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class HandleStates
{
    public class InputState
    {
        public int tick;
        public Vector2 moveInput;
        public float MoveSpeed;
    }

    [System.Serializable]
    public class TransformStateRW : INetworkSerializable, IEquatable<HandleStates.TransformStateRW>
    {
        public int tick;
        public Vector3 finalPos;
        public Quaternion finalRot;
        public float moveSpeed;
        public bool isMoving;

        public bool Equals(TransformStateRW other)
        {
            throw new NotImplementedException();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T: IReaderWriter
        {
            /*serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref finalPos);
            serializer.SerializeValue(ref finalRot);
            serializer.SerializeValue(ref moveSpeed);
            serializer.SerializeValue(ref isMoving);*/
            if(serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out tick);
                reader.ReadValueSafe(out finalPos);
                reader.ReadValueSafe(out finalRot);
                reader.ReadValueSafe(out moveSpeed);
                reader.ReadValueSafe(out isMoving);
            }
            else
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(tick);
                writer.WriteValueSafe(finalPos);
                writer.WriteValueSafe(finalRot);
                writer.WriteValueSafe(moveSpeed);
                writer.WriteValueSafe(isMoving);
            }
        }
    }
}

public class PlayerNetworkMovement : NetworkBehaviour
{
    CharacterController charController;
    bool _bCanMove;
    [SerializeField] private CinemachineCamera _VCam;
    [SerializeField] private Camera _cam;
    [SerializeField] float _turnSmoothVelocity;
    [SerializeField] float _turnSmoothTime;
    [SerializeField] Animator _animator;
    [SerializeField] float _gravity;

    private int tick = 0; 
    private float tickRate = 1/60f;
    private float tickDeltaTime = 0;

    private const int buffer = 1024;

    private HandleStates.InputState[] _inputStates = new HandleStates.InputState[buffer];
    private HandleStates.TransformStateRW[] _transformStates = new HandleStates.TransformStateRW[buffer];
    public NetworkVariable<HandleStates.TransformStateRW> currentServerTransformState = new NetworkVariable<HandleStates.TransformStateRW>(
        new HandleStates.TransformStateRW{}
    );
    public HandleStates.TransformStateRW previousServerTransformState;

    private void Awake()
    {
        charController = GetComponent<CharacterController>();
    }

    private void OnServerStateChanged(HandleStates.TransformStateRW previousValue, HandleStates.TransformStateRW newValue)
    {
        previousServerTransformState = previousValue;
    }

    private void OnEnable()
    {
        currentServerTransformState.OnValueChanged += OnServerStateChanged;
    }

    public void ProcessLocalPlayerMovement(Vector2 rawInput, float moveSpeed)
    {
        tickDeltaTime += Time.deltaTime;
        if(tickDeltaTime > tickRate)
        {
            int bufferIndex = tick & buffer;

            MovePlayerWithServerTickServerRpc(tick, rawInput, moveSpeed);
            MovePlayer(rawInput, moveSpeed);
            
            HandleStates.InputState inputState = new()
            {
                tick = tick,
                moveInput = rawInput,
                MoveSpeed = moveSpeed
            };

            HandleStates.TransformStateRW transformState = new()
            {
                tick = tick,
                finalPos = transform.position,
                finalRot = transform.rotation,
                isMoving = true
            };

            _inputStates[bufferIndex] = inputState;
            _transformStates[bufferIndex] = transformState;

            tickDeltaTime -= tickRate;
            if(tick == buffer)
            {
                tick = 0;
            }
            else
            {
                tick++;
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerWithServerTickServerRpc(int _tick, Vector2 rawInput, float moveSpeed)
    {
        MovePlayer(rawInput, moveSpeed);
        HandleStates.TransformStateRW transformState = new()
        {
            tick = _tick,
            finalPos = transform.position,
            finalRot = transform.rotation,
            isMoving = true
        };
        previousServerTransformState = currentServerTransformState.Value;
        currentServerTransformState.Value = transformState;
    }

    public void SimulateOtherPlayers()
    {
        tickDeltaTime += Time.deltaTime;

        if(tickDeltaTime > tickRate)
        {
            if(currentServerTransformState.Value.isMoving)
            {
                transform.position = currentServerTransformState.Value.finalPos;
                transform.rotation = currentServerTransformState.Value.finalRot;
            }

            tickDeltaTime -= tickRate;
            if(tick == buffer)
            {
                tick = 0;
            }
            else
            {
                tick++;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _bCanMove = true;
    }

    public void MovePlayer(Vector2 rawInput, float moveSpeed)
    {
        float targetAngle = GetTargetAngle(rawInput);

        float rotAngle = GetRotationAngle(targetAngle);
        
        Move(targetAngle, rotAngle, moveSpeed);
        ApplyGravity();
        /*if(IsServer && IsLocalPlayer)
        {
            if(rawInput != Vector2.zero){
                Move(targetAngle, rotAngle, moveSpeed);
                ControlWalkingAnimation(true);
            }
            else
            {
                ControlWalkingAnimation(false);
            }
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
        ParseGravity();*/
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerServerRpc(float targetAngle, float rotationAngle, float moveSpeed)
    {
        Move(targetAngle, rotationAngle, moveSpeed);
    }

    private void Move(float targetAngle, float rotationAngle, float moveSpeed)
    {
        Quaternion newAngle = Quaternion.Euler(0f, rotationAngle, 0f);
        transform.rotation = newAngle;

        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        
        charController.Move(moveDir.normalized * (moveSpeed * tickRate));
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

    private void ParseGravity()
    {
        if(IsClient && IsLocalPlayer)
        {
            GravityServerRpc();
        }
        else if(IsServer && IsLocalPlayer)
        {
            ApplyGravity();
        }
    }

    [Rpc(SendTo.Server)]
    private void GravityServerRpc()
    {
        ApplyGravity();
    }

    private void ApplyGravity()
    {
        if(charController.isGrounded)
        {
            return;
        }

        charController.Move(Physics.gravity * tickRate);
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
