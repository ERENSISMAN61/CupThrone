using Unity.Netcode;
using UnityEngine;

public class DealDamageOnContact : MonoBehaviour
{

    [SerializeField] private int damage = 10;

    private ulong ownerClientId; // mermiyi atan sahibin client id'si olacak

    public void SetOwner(ulong ownerClientId) //Projectile Laucher kodundan bu metot çağrılacak. Yani client kendi id'sini bu scripte gönderecek.
    {
        this.ownerClientId = ownerClientId;
    }

    private void OnTriggerEnter(Collider collider)
    {
        //  Debug.Log("Trigger Enter");
        if (collider.transform.parent == null) { return; }
        //    Debug.Log("Trigger Enter 2");
        if (collider.attachedRigidbody == null) { return; }
        //   Debug.Log("Trigger Enter 3");

        if (collider.attachedRigidbody.TryGetComponent<NetworkObject>(out NetworkObject networkObj)) //network objesi var mı? var ise o biz miyiz?
        {
            //  Debug.Log("Network Object ben olabilirim.");
            if (networkObj.OwnerClientId == ownerClientId) { return; }// bizsek return et yyani işlem yapma.
                                                                      //  Debug.Log("Network Object ben değilim.");
        }

        if (collider.attachedRigidbody.TryGetComponent<Health>(out Health health))
        {
            //  Debug.Log("Health Component var. vuruyom");
            health.TakeDamage(damage);
        }

    }
}
