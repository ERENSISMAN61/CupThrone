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
        // Set Y position for all players regardless of ownership
        Vector3 position = transform.position;
        position.y = 30f;
        transform.position = position;
        Physics.gravity = new Vector3(0, -360.81f, 0);

        // Camera priority only for owner
        if (IsOwner)
        {
            virtualCamera.Priority = ownerPriority;
        }
    }
}
