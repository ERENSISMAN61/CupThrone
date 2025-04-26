using Unity.Netcode;
using UnityEngine;

public class ArrowWallet : NetworkBehaviour
{

    public NetworkVariable<int> TotalArrows = new NetworkVariable<int>();

    public void SpendCoins(int costToFire)
    {
        TotalArrows.Value -= costToFire;
    }

}
