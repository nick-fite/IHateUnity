using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerNetwork : MultiplayerActor, ITeamInterface
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
    [SerializeField] private GameObject _playerObj;


    ///[Additional Components]
    DamageComponent _damageComponent;

    [Header("Player Options")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 2f;
    Vector2 _rawMovementInput;
    bool _bCanMove = true; //<-- if we want to lock the player movement at some point
    [SerializeField] PlayerNetworkMovement playerNetworkMovement;

    private bool _bIsGrounded;
    private float _gravity = -9.81f;
    private Vector3 _playerVelocity;

    [Header("Interaction")]
    [SerializeField] float interactRadius;
    [SerializeField] Transform interactOrigin;
    private IinteractionInterface _targetInteractible;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera _VCam;
    [SerializeField] private AudioListener _listener; 
    
    

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
        _damageComponent = GetComponent<DamageComponent>();
        _characterController = _playerObj.GetComponent<CharacterController>();
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
        playerNetworkMovement.MovePlayer(_rawMovementInput, moveSpeed);
    }

    /*private void ProcessAllMovement(Vector2 rawInput, float movementSpeed)
    {
        ProcessAllMovementServerRpc(rawInput, movementSpeed);
    }
    [Rpc(SendTo.Server)]
    private void ProcessAllMovementServerRpc(Vector2 rawInput, float movementSpeed) 
    {
        ProcessMove(rawInput, moveSpeed);
    }
    
    /*[Rpc(SendTo.Everyone)]
    private void ProcessAllMovementClientRpc(Vector2 rawInput, float movementSpeed)
    {
        ProcessLocalMovement(rawInput, movementSpeed);
    }
    private void ProcessLocalMovement(Vector2 rawInput, float movementSpeed) 
    {
        ProcessMove(rawInput, movementSpeed);
        ProcessGravity();
    }
    private void ProcessGravity()
    {
        _playerVelocity.y += Time.deltaTime * _gravity;

        if (_bIsGrounded && _playerVelocity.y < 0)
        {
            _playerVelocity.y = -2f;
        }
        _characterController.Move(Time.deltaTime * _playerVelocity);
    }*/

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

        if(IsOwner)
        {
            _listener.enabled = true;
            _VCam.Priority = 1;
        }
        else
        {
            _VCam.Priority = 0;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SpawnPlayerServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayerServerRpc()
    {
        SpawnPlayerClientRpc();
    }
    [Rpc(SendTo.Everyone)]
    private void SpawnPlayerClientRpc()
    {
        List<GameObject> positions = new List<GameObject>();
        positions.Clear();
        positions.AddRange(GameObject.FindGameObjectsWithTag("SpawnPoints"));
        int rand = UnityEngine.Random.Range(0, positions.Count);
        Transform newPos = positions[rand].transform;
        NetworkTransform nt = GetComponent<NetworkTransform>();
        nt.Teleport(newPos.position, newPos.rotation, newPos.localScale);   
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        OnPlayerDisconnect?.Invoke();
    }

    public void MoveAction(InputAction.CallbackContext context) 
    {
        if (!IsLocalPlayer || !IsOwner) { return; }
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

        if (context.started)
        {
            TryInteract();
        }
    }
    private void TryInteract() 
    {
        if (_targetInteractible != null)//we can change this later if we need to
        {
            _targetInteractible.InteractAction(this.gameObject);
            _targetInteractible = null;
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(interactOrigin.position, interactRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            IinteractionInterface hitInteractionInterface = hitCollider.gameObject.GetComponent<IinteractionInterface>();
            if (hitInteractionInterface != null && hitInteractionInterface.ShouldInteract(this.gameObject))
            {
                _targetInteractible = hitInteractionInterface;
                hitInteractionInterface.InteractAction(this.gameObject);
                return;
            }
        }
        _targetInteractible = null;///If nothing is interactible, clear just in case
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref moveSpeed);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactOrigin.position, interactRadius);
    }
}
