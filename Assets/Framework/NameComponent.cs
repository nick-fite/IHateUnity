using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct NetworkString : INetworkSerializeByMemcpy
{
    private ForceNetworkSerializeByMemcpy<FixedString32Bytes> _fixedString;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _fixedString);
    }

    public override string ToString()
    {
        return _fixedString.Value.ToString();
    }

    public static implicit operator string(NetworkString networkString) => networkString.ToString();
    public static implicit operator NetworkString(string stringToConvert) => new NetworkString()
    {
        _fixedString = new FixedString32Bytes(stringToConvert)
    };
}

public class NameComponent : NetworkBehaviour, INetworkSerializable
{
    public delegate void OnNameChangedDelegate(string newName);
    public OnNameChangedDelegate OnNameChanged;
    public OnNameChangedDelegate OnDisplayNameChanged;

    private NetworkString _playerName;

    private void Start()
    {
        OnNameChanged += ChangeName;
    }
    public void RefreshName() 
    {
        ChangeName(_playerName.ToString());
    }

    public void ChangeName(string newName)
    {
        if (IsLocalPlayer)
        {
            UpdateNameServerRpc(newName);
        }
    }
    [Rpc(SendTo.Server)]
    public void UpdateNameServerRpc(NetworkString newName)
    {
        UpdateNameClientRpc(newName);
    }
    [Rpc(SendTo.Everyone)]
    public void UpdateNameClientRpc(NetworkString newName)
    {
        UpdateName(newName);
    }
    public void UpdateName(NetworkString newName)
    {
        _playerName = newName;
        OnDisplayNameChanged?.Invoke(_playerName);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _playerName);
    }
}
