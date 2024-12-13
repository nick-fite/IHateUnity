using System;
using System.Collections;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using Object = UnityEngine.Object;

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
    private MultiplayerInputAction _multiplayerInputAction;
    private CharacterController _characterController;
    [SerializeField] private GameObject _playerObj;


    ///[Additional Components]
    DamageComponent _damageComponent;

    [Header("Player Options")]
    [SerializeField] private float playerMovementOnStartDelay = 5f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 2f;
    Vector2 _rawMovementInput;

    private NetworkVariable<bool> _bCanMove = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] PlayerNetworkMovement playerNetworkMovement;

    private bool _bIsGrounded;

    [Header("Interaction")]
    [SerializeField] float attackRadius = 1f;
    [SerializeField] float interactRadius = 1f;
    [SerializeField] Transform interactOrigin;
    private IinteractionInterface _targetInteractible;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera virtualCam;
    [SerializeField] private AudioListener audioListener; 
    
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

        _bCanMove.Value = false;

        _animator = GetComponentInChildren<Animator>();
        _damageComponent = GetComponent<DamageComponent>();
        _characterController = _playerObj.GetComponent<CharacterController>();
    }
    
    public void Start()
    {
        _swordHash = Animator.StringToHash("sword");
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
        GetComponent<HealthComponent>().OnDead += ResetPlayerParse;
        StartCoroutine(DelayThenMove());


        GameObject networkUIObject = GameObject.FindGameObjectWithTag("NetworkManagerUI");
        if (!networkUIObject)
        {
            Debug.Log("Network NotFound");
            return;
        }
        NetworkManagerUI networkUI = networkUIObject.GetComponent<NetworkManagerUI>();
        if (networkUI)
        {
            networkUI.SetCurrentPlayerName();
        }
    }
    private void Update()
    {
        if (!_characterController) { return; }

        _bIsGrounded = _characterController.isGrounded;
    }

    private void FixedUpdate()
    {
        if (!_characterController || !_bCanMove.Value) { return; }
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
            audioListener.enabled = true;
            virtualCam.Priority = 1;
        }
        else
        {
            virtualCam.Priority = 0;
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
        _bCanMove.Value = false;
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
        _bCanMove.Value = true;
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
            _animator.SetTrigger(_attackHash);
        }
    }
    public void LeaveAction(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer) { return; }
        if (context.started)
        { 
            Application.Quit();
            Debug.Log("Quit");
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
        _animator.SetTrigger(_pickUpHash);
        if (_targetInteractible != null)
        {
            _targetInteractible.InteractAction(this.gameObject);
            _targetInteractible = null;
            _animator.SetTrigger(_throwHash);
            ResetAllAnimationServerRpc();
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(interactOrigin.position, interactRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            IinteractionInterface hitInteractionInterface = hitCollider.gameObject.GetComponent<IinteractionInterface>();
            if (hitInteractionInterface != null && hitInteractionInterface.ShouldInteract(this.gameObject))
            {
                _targetInteractible = hitInteractionInterface;
                GameObject gameObj = hitCollider.gameObject;
                gameObj.GetComponent<Collider>().isTrigger = true;
                gameObj.GetComponent<Rigidbody>().isKinematic = true;
                if(IsClient && IsLocalPlayer)
                {
                    SetNewAnimationServerRpc(gameObj.GetComponent<Item>().newAnimSet);
                }
                else if (IsServer && IsLocalPlayer)
                {
                    SetNewAnimation(gameObj.GetComponent<Item>().newAnimSet);
                }

                hitInteractionInterface.InteractAction(this.gameObject);
                return;
                
            }
        }
        _targetInteractible = null;///If nothing is interactible, clear just in case
    }

    private void ResetPlayerParse()
    {
        if(IsClient && IsLocalPlayer)
        {
            ResetPlayerRpc(); 

        }
        else if (IsServer && IsLocalPlayer)
        {
            ResetPlayer();

        }
        TryInteract();
        StartCoroutine(DelayThenMove());
    }

    [Rpc(SendTo.Server)]
    private void ResetPlayerRpc()
    {
        Debug.LogWarning("SPAWNING");
        ResetPlayer();

    }

    private void ResetPlayer()
    {
        _characterController.transform.position = Vector3.zero;
        GetComponent<HealthComponent>().ChangeHealth(100);
    }

    [Rpc(SendTo.Server)]
    private void ResetAllAnimationServerRpc()
    {
        ResetAllAnimation();
    }

    private void ResetAllAnimation()
    {
        _animator.SetBool(_hookshotHash, false);
        _animator.SetBool(_swordHash, false);
        _animator.SetBool(_holdingHash, false);
    }

    [Rpc(SendTo.Server)]
    private void SetNewAnimationServerRpc(string newAnim)
    {
        SetNewAnimation(newAnim);
    }

    private void SetNewAnimation(string newAnim)
    {
        ResetAllAnimation();
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

