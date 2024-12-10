using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TestPlayerNetwork : NetworkBehaviour, ITeamInterface
{
    public delegate void OnPlayerDisconnectDelegate();
    public OnPlayerDisconnectDelegate OnPlayerDisconnect;

    public delegate void OnNameChangedDelegate(string newName);
    public OnNameChangedDelegate OnNameChanged;

    [SerializeField] Animator anim;
    [SerializeField] BoxTriggerDamageComponent damageComponent;
    [SerializeField] float turnSpeed;

    public void Start()
    {
        anim = GetComponentInChildren<Animator>();
        Debug.Log(anim.name);
        damageComponent = GetComponent<BoxTriggerDamageComponent>();

        NetworkManagerUI networkUI = FindFirstObjectByType<NetworkManagerUI>();
        string playerName = networkUI.GetPlayerName();
        GetComponent<TestPlayerNetwork>().OnNameChanged?.Invoke(playerName);
        Debug.Log(playerName);
    }

    void FixedUpdate()
    {
        Vector2 input = Vector2.zero;

        if(Input.GetKey(KeyCode.W)) input.y += 1f;
        if(Input.GetKey(KeyCode.S)) input.y -= 1f;
        if(Input.GetKey(KeyCode.D)) input.x += 1f;
        if(Input.GetKey(KeyCode.A)) input.x -= 1f;

        if(IsServer && IsLocalPlayer)
        {
            Move(input);
            RotFaceDirection(input);
        } else if (IsClient && IsLocalPlayer)
        {
            MovePlayerServerRpc(input);
            RotFaceDirectionServerRpc(input);
        }

        if (damageComponent && Input.GetKey(KeyCode.E)) 
        {
            Debug.Log("Try Damage");
            damageComponent.DoDamage();
        }

    }

    [ServerRpc]
    public void MovePlayerServerRpc (Vector2 input)
    {
        Move(input);
    }


    public void Move(Vector2 input){
        if(input.x > 0 || input.x < 0 || input.y > 0 || input.y < 0)
        {
            anim.SetBool("walking", true);
        }
        else
            anim.SetBool("walking", false);

        float moveSpeed = 3f;
        Vector3 calcMove = input.x * transform.right + input.y * transform.forward;
        transform.position += calcMove * moveSpeed * Time.fixedDeltaTime;
    }

    [ServerRpc]
    public void RotFaceDirectionServerRpc(Vector2 input)
    { 
        RotFaceDirection(input);
    }
    private void RotFaceDirection(Vector2 input)
    {
        /*Vector3 movementVal = new Vector3(input.x, input.y, 0);
        Vector3 rotInDir = transform.TransformDirection(movementVal);
        Quaternion goalRot = Quaternion.LookRotation(rotInDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, goalRot, Time.deltaTime * turnSpeed);*/
    }

    public override void OnNetworkDespawn() 
    {
        OnPlayerDisconnect?.Invoke();
    }
}
