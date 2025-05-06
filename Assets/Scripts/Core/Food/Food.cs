using Unity.Netcode;
using UnityEngine;

public abstract class Food : NetworkBehaviour// child classlar artık NetworkBehaviour'a sahip olacak.
                                             //ve IsServer gibi metodları kullanabilecek alt classlar.
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private ConsumableItem foodItem;

    //protected özellikler inspectorde görünmez.
    protected int foodValue = 5; //protected olmasının sebebi, bu değeri sadece bu class ve bu class'ı inherit eden classlar kullanabilecek.
    protected bool alreadyCollected;//default olarak false olur her bool.



    //abstract methodlar sadece abstract classlarda olabilir.
    // Bu methodu inherit eden classlar kendi içerisinde implement etmek zorundadır.
    public abstract int Collect();

    public void SetValue(int value) // abstract olmayan metodlar child classlarda override edilmek zorunda değildir.
    //yani bu metodu direkt bu classtan çekip kullanacağız. yeniden metod yazmayacağız. ör: SetValue(5); gibi
    {
        foodValue = value;
    }

    protected void Show(bool show)
    {
        meshRenderer.enabled = show;
    }

    public ConsumableItem GetFoodItem()
    {
        return foodItem;
    }
    
    // Make this method handle network synchronization of the food collection
    [ServerRpc(RequireOwnership = false)]
    public void CollectServerRpc(ulong playerId)
    {
        if (alreadyCollected) return;
        
        // Mark as collected and hide the food
        alreadyCollected = true;
        Show(false);
        
        // Call Collect to trigger any additional logic in child classes
        int foodValue = Collect();
        
        // Notify all clients that this food was collected
        CollectClientRpc();
    }
    
    [ClientRpc]
    private void CollectClientRpc()
    {
        // Update visual state on all clients
        Show(false);
        alreadyCollected = true;
    }
}
