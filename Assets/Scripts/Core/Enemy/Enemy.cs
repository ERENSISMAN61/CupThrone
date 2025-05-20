using Unity.Netcode;
using UnityEngine;

public abstract class Enemy : NetworkBehaviour
{

    [SerializeField] private Renderer[] renderers; // Yeni çoklu renderer dizisi - MeshRenderer ve SkinnedMeshRenderer dahil

    protected bool isDead = false; // Düşman öldü mü

    // Düşman yenildiğinde çağrılan metot
    public abstract void OnDefeated();


    protected void SetVisible(bool visible)
    {

        // Yeni çoklu renderer kontrolü
        if (renderers != null && renderers.Length > 0)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }
    }

    [ClientRpc]
    protected void SetVisibleClientRpc(bool visible)
    {
        SetVisible(visible);
    }
}