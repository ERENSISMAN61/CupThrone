using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class TankPlayer : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Settings")]
    [SerializeField] private int ownerPriority = 15;

    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            virtualCamera.Priority = ownerPriority;
        }
    }
}
