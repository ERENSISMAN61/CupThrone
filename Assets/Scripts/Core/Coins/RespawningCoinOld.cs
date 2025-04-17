// using System;
// using NUnit.Framework;
// using UnityEngine;

// public class RespawningCoin : Coin //RespawningCoin, Coin class'ından inherit edilmiştir.
// {
//     public event Action<RespawningCoin> OnCollected;
//     private Vector3 previousPosition;

//     private void Update()
//     {
//         if (previousPosition != transform.position) // para ilk bir poziyonda zaten görünür. eğer para toplanırsa para gizlenir o sırada başka yere ışınlıyoruz.
//         //bu kodda ise yeni transform pozsiyonu değiştiği yerde para görünür olsun. en başta para alınmadan önce pozisyonu previousa tanımlıyor burda. daha sonra sürekli update ediyor o transtorm
//         //değiştiği an show true yaparak para görünür oluyor.
//         {
//             Show(true);
//         }

//         previousPosition = transform.position;
//     }
//     public override int Collect()
//     {
//         if (!IsServer) //Client hemen parayı gizler ve bu metoddan çıkar.
//         //Kullanıcı deneyimi için kullanıcıya hızlı parannın kaybolması önnemli.
//         {

//             Show(false);
//             return 0;
//         }

//         //bu kısımdan aşağısı server tarafında çalışır. client sadece üstkısmı çalıştırır. if bloğundan dolayı.
//         //bu sayede para toplama işi sadece server tarafında yapılır. client sadece görsel işlemleri yapar.
//         //eğer para toplanacaksa server aşağıda olduğu gibi coinValue'yu döndürür.

//         if (alreadyCollected) { return 0; }

//         alreadyCollected = true;

//         OnCollected?.Invoke(this);

//         return coinValue;
//     }

//     public void Reset()
//     {
//         alreadyCollected = false;
//     }

// }
