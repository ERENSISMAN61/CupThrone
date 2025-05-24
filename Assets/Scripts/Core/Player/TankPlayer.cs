using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
using UnityEditor.Rendering;

public class TankPlayer : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Settings")]
    [SerializeField] private int ownerPriority = 15;
    [SerializeField] private float heightOffset = 10.0f; // Height above terrain
    [SerializeField] private Rigidbody rb;
    public override void OnNetworkSpawn()
    {

        // Camera priority only for owner
        if (IsOwner)
        {
            virtualCamera.Priority = ownerPriority;
        }

        StartCoroutine(WaitForTerrain());

    }

    private IEnumerator WaitForTerrain()
    {
        // set the players gravity to 0
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        //set players position to the terrain height + offset using raycast
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity))
        {
            // Set the player's position to the terrain height + offset
            transform.position = new Vector3(transform.position.x, hit.point.y + heightOffset, transform.position.z);
        }
        // Set the player's rotation to the terrain normal
        transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;

        // Wait for 1 second
        yield return new WaitForSeconds(1f);

        // Set the player's gravity to 1
        rb.useGravity = true;
        rb.isKinematic = false;

    }


}
