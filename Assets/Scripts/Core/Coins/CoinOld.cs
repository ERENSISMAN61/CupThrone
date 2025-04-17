// using Unity.Netcode;
// using UnityEngine;

// public abstract class Coin : NetworkBehaviour// child classlar artık NetworkBehaviour'a sahip olacak.
//                                              //ve IsServer gibi metodları kullanabilecek alt classlar.
// {
//     [SerializeField] private MeshRenderer meshRenderer;

//     //protected özellikler inspectorde görünmez.
//     protected int coinValue = 5; //protected olmasının sebebi, bu değeri sadece bu class ve bu class'ı inherit eden classlar kullanabilecek.
//     protected bool alreadyCollected;//default olarak false olur her bool.



//     //abstract methodlar sadece abstract classlarda olabilir.
//     // Bu methodu inherit eden classlar kendi içerisinde implement etmek zorundadır.
//     public abstract int Collect();

//     public void SetValue(int value) // abstract olmayan metodlar child classlarda override edilmek zorunda değildir.
//     //yani bu metodu direkt bu classtan çekip kullanacağız. yeniden metod yazmayacağız. ör: SetValue(5); gibi
//     {
//         coinValue = value;
//     }

//     protected void Show(bool show)
//     {
//         meshRenderer.enabled = show;
//     }

// }
