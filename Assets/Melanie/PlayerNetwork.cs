using System;
using System.Collections;
using Unity.Cinemachine;
using Unity.Mathematics;
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
    [SerializeField] float attackRadius = 1f;
    [SerializeField] float interactRadius = 1f;
    [SerializeField] Transform interactOrigin;
    private IinteractionInterface _targetInteractible;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera _VCam;
    [SerializeField] private AudioListener _listener; 
    

        private int _walkingHash;
        private int _swordHash;
        private int _hookshotHash;
        private int _holdingHash;
        private int _attackHash;
        private int _throwHash;
        private int _pickUpHash;

    private void Awake()
    {
        OnUpdateAllConnections += UpdateAllConnections;

        _multiplayerInputAction = new MultiplayerInputAction();

        _playerInput = GetComponent<PlayerInput>();
        _bCanMove = true;

        _animator = GetComponentInChildren<Animator>();
        _damageComponent = GetComponent<DamageComponent>();
        _characterController = _playerObj.GetComponent<CharacterController>();
    }
    
    public void Start()
    {
        _walkingHash = Animator.StringToHash("walking");
        _swordHash = Animator.StringToHash("Sword");
        _hookshotHash = Animator.StringToHash("hookshot");
        _holdingHash = Animator.StringToHash("holding");
        _attackHash = Animator.StringToHash("attack");
        _throwHash = Animator.StringToHash("throw");
        _pickUpHash = Animator.StringToHash("pickUp");
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
        
        if (IsLocalPlayer)
        { 
            _multiplayerInputAction.Disable();
        }
        StartCoroutine(DelayThenMove());
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
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        OnPlayerDisconnect?.Invoke();
    }

    IEnumerator DelayThenMove()
    {
        yield return new WaitForSeconds(5);
        ParseMoveToSpawnPos();
        if (IsLocalPlayer)
        { 
            _multiplayerInputAction.Disable();
        }
    }

    private void ParseMoveToSpawnPos()
    {
        if(IsServer && IsLocalPlayer)
        {
            MoveToSpawnPos();
        }
        if(IsClient && IsLocalPlayer)
        {
            MoveToSpawnPosServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void MoveToSpawnPosServerRpc()
    {
        MoveToSpawnPos();
    }

    private void MoveToSpawnPos()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoints");
        Transform newSpawn = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform;
        _characterController.transform.position = newSpawn.position;
        _characterController.transform.rotation = newSpawn.rotation;
    }

    public Vector3 GetPlayerUp()
    {
        return _characterController.transform.up;
    }

    public Vector3 GetPlayerForward()
    {
        return _characterController.transform.forward;
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
                GameObject obj = hitCollider.gameObject;
                obj.GetComponent<Rigidbody>().isKinematic = true;
                obj.GetComponent<Collider>().isTrigger = true;
                ParseAnimations(obj.GetComponent<Item>().newAnimSet);
                hitInteractionInterface.InteractAction(this.gameObject);
                return;
            }
        }
        _targetInteractible = null;///If nothing is interactible, clear just in case
    }

    void ParseAnimations(string newAnim)
    {
        _animator.SetBool(_swordHash, false);
        _animator.SetBool(_hookshotHash, false);
        _animator.SetBool(_holdingHash, false);
        
        _animator.SetBool(newAnim, true);
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

