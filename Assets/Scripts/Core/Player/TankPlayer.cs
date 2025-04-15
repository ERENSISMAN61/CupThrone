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
        // // Set Y position for all players regardless of ownership
        // Vector3 position = transform.position;
        // position.y = 500f;
        // transform.position = position;


        // Camera priority only for owner
        if (IsOwner)
        {
            virtualCamera.Priority = ownerPriority;
        }
    }

    private void Start()
    {
        Vector3 position = transform.position;
        position.y = 500f;
        transform.position = position;

    }
}
