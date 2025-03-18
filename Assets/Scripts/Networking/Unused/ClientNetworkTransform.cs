using System.Collections; // Koleksiyon sınıflarını kullanmak için gerekli
using System.Collections.Generic; // Generic koleksiyonları kullanmak için gerekli
using Unity.Netcode.Components; // Unity Netcode bileşenlerini içe aktarıyoruz
using UnityEngine; // UnityEngine sınıflarını içe aktarıyoruz

// NetworkTransform sınıfından miras alan yeni bir sınıf tanımlıyoruz
public class ClientNetworkTransform : NetworkTransform
{
    // Nesne ağ üzerinde oluşturulduğunda çağrılan metod
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // Üst sınıfın OnNetworkSpawn metodunu çağırıyoruz
        CanCommitToTransform = IsOwner; // Eğer nesnenin sahibiysek, transform değişikliklerini yapabiliriz
    }

    // Her karede çağrılan Update metodunu tanımlıyoruz
    private void Update()
    {
        CanCommitToTransform = IsOwner; // Sahipliğe göre transform yapma yeteneğimizi güncelliyoruz

        // Eğer NetworkManager mevcutsa
        if (NetworkManager != null)
        {
            // Bağlı bir istemciysek veya sunucu dinliyorsa
            if (NetworkManager.IsConnectedClient || NetworkManager.IsListening)
            {
                // Transform değişikliklerini yapma yetkimiz varsa
                if (CanCommitToTransform)
                {
                    // TryCommitTransformToServer yerine SetState kullanıyoruz
                    // Mevcut pozisyon, dönüş ve ölçek değerlerini sunucuya gönderiyoruz
                    SetState(transform.position, transform.rotation, transform.localScale);
                }
            }
        }
    }

    // Sunucunun mı yoksa istemcinin mi yetkili olduğunu belirlemek için bu metodu geçersiz kılıyoruz
    protected override bool OnIsServerAuthoritative()
    {
        return false; // False döndürerek istemcinin yetkili olduğunu belirtiyoruz
    }
}
