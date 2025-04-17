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
        // Position player on terrain
        PositionOnTerrain();

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
        Vector3 currentPosition = transform.position;
        
        // Start raycast from high above the current XZ position
        Vector3 rayStart = new Vector3(currentPosition.x, 100f, currentPosition.z);
        Ray ray = new Ray(rayStart, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, terrainLayerMask))
        {
            // Log every raycast hit
            Debug.Log($"Player raycast hit: {hit.collider.name} at position {hit.point} with distance {hit.distance}");
            
            // Position player slightly above the terrain surface
            Vector3 newPosition = hit.point;
            newPosition.y += heightOffset;
            transform.position = newPosition;
            
            // Optional: Align rotation with terrain normal
            // transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("Could not find terrain below player. Using default position.");
            }
        }
    }
}
