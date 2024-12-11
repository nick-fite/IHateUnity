using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerNetwork : NetworkBehaviour, ITeamInterface
{
    public delegate void OnUpdateAllConnectionsDelegate();
    public OnUpdateAllConnectionsDelegate OnUpdateAllConnections;

    public delegate void OnPlayerDisconnectDelegate();
    public OnPlayerDisconnectDelegate OnPlayerDisconnect;

    ///[Actions and Animator dependencies]
    Animator _animator;
    PlayerInput _playerInput;
    private MultiplayerInputAction _multiplayerInputAction;
    private CharacterController _characterController;


    ///[Additional Components]
    DamageComponent _damageComponent;

    [Header("Player Options")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 2f;
    Vector2 _rawMovementInput;
    bool _bCanMove = true; //<-- if we want to lock the player movement at some point

    private bool _bIsGrounded;
    private float _gravity = -9.81f;
    private Vector3 _playerVelocity;

    [Header("Other [Read Only]")]
    [SerializeField ]private IinteractionInterface _targetInteractible;

    public void SetPlayerVelocity(Vector3 velocityToSet) 
    {   
        _playerVelocity = velocityToSet;
    }
    public void SetTargetInteractible(GameObject objectToSet) 
    {
        if (objectToSet == null)
        {
            _targetInteractible = null;
            return;
        }
        IinteractionInterface targetInteractible = objectToSet.GetComponent<IinteractionInterface>();
        if (targetInteractible == null) 
        {
            _targetInteractible = null;
            return;
        }
        _targetInteractible = targetInteractible;
    }

    private void Awake()
    {
        OnUpdateAllConnections += UpdateAllConnections;

        _multiplayerInputAction = new MultiplayerInputAction();

        if (IsLocalPlayer)
        { 
            _multiplayerInputAction.Enable();
        }
        _playerInput = GetComponent<PlayerInput>();
        _bCanMove = true;

        _animator = GetComponentInChildren<Animator>();
        _characterController = GetComponent<CharacterController>();
        _damageComponent = GetComponent<DamageComponent>();
    }
    
    public void Start()
    {
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            GameObject otherPlayer = client.PlayerObject.gameObject;
            if (!otherPlayer || otherPlayer == gameObject)
            {
                continue;
            }
            Debug.Log(otherPlayer.name);
            PlayerNetwork playerNetwork = otherPlayer.GetComponent<PlayerNetwork>();
            if (playerNetwork)
            {
                Debug.Log("Player network test found!!");
                playerNetwork.OnUpdateAllConnections?.Invoke();
            }
        }
    }
    private void Update()
    {
        if (!_characterController) { return; }

        _bIsGrounded = _characterController.isGrounded;
    }

    private void FixedUpdate()
    {
        if (!_characterController) { return; }
        ProcessAllMovement(_rawMovementInput, moveSpeed);
    }

    private void ProcessAllMovement(Vector2 rawInput, float movementSpeed)
    {
        if (IsServer && IsLocalPlayer)
        {
            ProcessAllMovementServerRpc(rawInput, movementSpeed);
        }
        else if (IsClient && IsLocalPlayer)
        { 
            ProcessAllMovementServerRpc(rawInput, movementSpeed);
        }
    }
    [Rpc(SendTo.Server)]
    private void ProcessAllMovementServerRpc(Vector2 rawInput, float movementSpeed) 
    {
        ProcessAllMovementClientRpc(rawInput, movementSpeed);
    }
    [Rpc(SendTo.Everyone)]
    private void ProcessAllMovementClientRpc(Vector2 rawInput, float movementSpeed)
    {
        ProcessLocalMovement(rawInput, movementSpeed);
    }
    private void ProcessLocalMovement(Vector2 rawInput, float movementSpeed) 
    {
        ProcessMove(rawInput, movementSpeed);
        ProcessGravity();
    }

    private void ProcessMove(Vector2 rawInput, float movementSpeed)
    {
        if (!_bCanMove) { return; }

        //Moving character
        Vector3 movementValue = new Vector3(rawInput.x, 0, rawInput.y);
        Vector3 moveInDirection = transform.TransformDirection(movementValue);
        _characterController.Move(moveInDirection * (movementSpeed * Time.fixedDeltaTime));

        //animation
        if (!_animator) { return; }

        if (movementValue != Vector3.zero)
        {
            _animator.SetBool("walking", true);
        }
        else
        { 
            _animator.SetBool("walking", false);
        }

        //rotating whole character in movement direction
        /*Quaternion goalRotation = Quaternion.LookRotation(moveInDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, goalRotation, Time.deltaTime * turnSpeed);*/
    }
    private void ProcessGravity()
    {
        _playerVelocity.y += Time.deltaTime * _gravity;

        if (_bIsGrounded && _playerVelocity.y < 0)
        {
            _playerVelocity.y = -2f;
        }
        _characterController.Move(Time.deltaTime * _playerVelocity);
    }

    private void UpdateAllConnections()
    {
        NameComponent nameComponent = GetComponent<NameComponent>();
        if (nameComponent)
        {
            nameComponent.RefreshName();
        }
        HealthComponent healthComponent = GetComponent<HealthComponent>();
        if (healthComponent) 
        {
            healthComponent.RefreshHealth();
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsLocalPlayer)
        {
            _multiplayerInputAction.Disable();
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        OnPlayerDisconnect?.Invoke();
    }

    public void MoveAction(InputAction.CallbackContext context) 
    {
        if (!IsLocalPlayer) { return; }
        if (context.performed)
        {
            _rawMovementInput = context.ReadValue<Vector2>();
        }
        if (context.canceled)
        {
            _rawMovementInput = Vector2.zero;
        }
    }
    public void AttackAction(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer) { return; }

        if (context.started && _damageComponent)
        {
            _damageComponent.DoDamage();
        }
    }
    public void InteractAction(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer) { return; }

        if (context.started && _targetInteractible != null)
        {
            _targetInteractible.InteractAction(this.gameObject);
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref moveSpeed);
    }
}
