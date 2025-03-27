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
        if (IsOwner)
        {
            virtualCamera.Priority = ownerPriority;

            // Ensure player spawns at Y=10 regardless of where it was instantiated
            Vector3 position = transform.position;
            position.y = 30f;
            transform.position = position;
            Physics.gravity = new Vector3(0, -360.81f, 0);
        }
    }
}
