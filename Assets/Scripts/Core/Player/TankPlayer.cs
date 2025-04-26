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
    [SerializeField] private LayerMask terrainLayerMask;
    [SerializeField] private float heightOffset = 1.0f; // Height above terrain
    [SerializeField] private bool showDebugInfo = false;

    public override void OnNetworkSpawn()
    {
        StartCoroutine(WaitForTerrainAndPosition());

        // Camera priority only for owner
        if (IsOwner)
        {
            virtualCamera.Priority = ownerPriority;
        }
    }

    private void Start()
    {
        PositionOnTerrain();
    }

    private void PositionOnTerrain()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
        {
            // Set the player's position above the terrain
            transform.position = new Vector3(transform.position.x, hit.point.y + heightOffset, transform.position.z);
        }
    }

    private IEnumerator WaitForTerrainAndPosition()
    {
        // Wait until the terrain is spawned
        while (!IsTerrainSpawned())
        {
            yield return null;
        }

        // Position player on terrain
        PositionOnTerrain();
    }

    private bool IsTerrainSpawned()
    {
        // Replace this with the actual logic to check if the terrain is spawned
        return Physics.Raycast(Vector3.zero + Vector3.up * 100f, Vector3.down, Mathf.Infinity, terrainLayerMask);
    }
}
