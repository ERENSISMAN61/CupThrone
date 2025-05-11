using System;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{

    [field: SerializeField] public int MaxHealth { get; private set; } = 100;
    //programcılar bu şekil özellik kullanmayı daha çok yapıyorlarmış alttakine göre.
    //Daha okunabilir oluyomuş sadece ve daha kullanışlıymış bu şekil get seti ayarlamak private public gibi.
    /* bunla aynı işlevi görüyor nerdeyse. Üstteki özellik alttaki ise alan diye geçiyor. 
    [SerializeField] private int MaxHealth = 100;
    
    public int getMaxHealth()
    {
    return MaxHealth;
    }

    */
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();
    //CurrentHealth değişkeni, NetworkVariable<int> tipinde. Bu değişkenin değeri, ağ üzerinden senkronize edilir.
    //Client bu değişkeni istese de değiştiremez. Sadece server değiştirebilir.
    private bool isDead;
    public Action<Health> OnDie;//OnDie olayı. Bu olaya abone olunabilir.

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; } //bu sayede tüm scripti sadece server çalıştırabilecek. Clientlar bu scripti çalıştıramayacak.

        CurrentHealth.Value = MaxHealth;
    }

    public void TakeDamage(int damage)
    {
        //Debug.Log("TakeDamage metodu çalıştı.");
        ModifyHealth(-damage);
    }

    public void RestoreHealth(int value)
    {
        ModifyHealth(value);
    }

    private void ModifyHealth(int value)
    {
        //Debug.Log("ModifyHealth metodu çalıştı.");
        if (isDead)
        {
            //Debug.Log("isDead true, return edildi");
            return;
        }

        int newHealth = CurrentHealth.Value + value;

        CurrentHealth.Value = Mathf.Clamp(newHealth, 0, MaxHealth);

        if (CurrentHealth.Value == 0)
        {
            isDead = true;



            OnDie?.Invoke(this); //OnDie olayına en az bir abone varsa(?: var mı yok mu check), olayı tetikler.
        }
    }

}
