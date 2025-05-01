using Unity.Netcode;
using UnityEngine;

public abstract class Enemy : NetworkBehaviour
{

    [SerializeField] private Renderer[] renderers; // Yeni çoklu renderer dizisi - MeshRenderer ve SkinnedMeshRenderer dahil

    [SerializeField] protected int health = 100; // Düşmanın sağlık değeri
    [SerializeField] protected int damage = 10; // Düşmanın verdiği hasar miktarı
    protected bool isDead = false; // Düşman öldü mü

    // Düşman yenildiğinde çağrılan metot
    public abstract void OnDefeated();

    public int GetDamage()
    {
        return damage;
    }

    public bool TakeDamage(int damageAmount)
    {
        if (isDead) return false;

        health -= damageAmount;

        if (health <= 0)
        {
            isDead = true;
            OnDefeated();
            return true;
        }

        return false;
    }

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
}