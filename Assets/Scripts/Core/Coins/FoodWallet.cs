using Unity.Netcode;
using UnityEngine;

public class FoodWallet : NetworkBehaviour
{

    public NetworkVariable<int> TotalFoods = new NetworkVariable<int>();

    public void SpendCoins(int costToFire)
    {
        TotalFoods.Value -= costToFire;
    }
    private void OnTriggerEnter(Collider collider)
    {

        if (!collider.TryGetComponent<Food>(out Food food)) { return; }

        int foodValue = food.Collect(); //Coin class'ını coin olarak çalıştırabilmemizin nedeni,
                                        //if bloğunda Coin class'ını coin olarak atamıştık. 
                                        //şu an bu kısım else bloğunda çalıştığı için coin class'ını coin olarak çalıştırabiliyoruz.


        if (!IsServer) { return; } // aşağıdaki Total coini sadece serverda arttırma işlemini yaptıracağız
                                   // o yüzden bu if bloğu koyduk.
                                   //!!Neden bu satır en üstte değil?
                                   //Çünkü bu satırın üstünde coin.Collect() metodunu client ayrı server ayrı yorumluyor.
                                   //Collect metoduna bakarsak client para gizlemekle ilgileniyor server para eklemekle.


        TotalFoods.Value += foodValue;
    }
}
