using System.Threading.Tasks;
using UnityEngine;

public class ClientSingleton : MonoBehaviour
{
    private static ClientSingleton instance; //Alan (Field), veriyi saklayan temel bir değişkendir.

    public ClientGameManager GameManager { get; private set; } //GameManager'ı almak ve ayarlamak için kullanılan özellik.

    public static ClientSingleton Instance//Özellik (Property), veriyi almak veya ayarlamak için kullanılan ve genellikle alanlar üzerinde bir erişim kontrolü 
                                          //veya iş kuralı uygulayan bir yöntemdir.
                                          //Özellikler, bir alanın (field) değerini almak ve ayarlamak için kullanılan yöntemlerdir. 
                                          //Alanların aksine, bir işlevsellik içerirler ve get ve set erişimcileri ile tanımlanırlar.

    {
        get
        {
            if (instance != null) { return instance; }//instance null değilse instance'ı döndür.

            instance = FindFirstObjectByType<ClientSingleton>();//instance null ise FindFirstObjectByType fonksiyonunu çağır ve instance'a atama yap.

            if (instance == null) //instance hala null ise hata mesajı ver.
            {
                Debug.LogError("There is no ClientSingleton in the scene!");
                return null;
            }

            return instance; //bulduğun instance'ı döndür.
        }
    }

    private void Start()
    {

        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> CreateClient() // neden async, task , await kullanılıyor?Neden InitAsync fonksiyonu async bir fonksiyon? 
    //Çünkü InitAsync fonksiyonu async bir fonksiyon çünkü bu fonksiyonun içinde await kullanılarak asenkron işlemler yapılıyor. asenkron ne iş yapılıyor?
    //!!asenkron işlemler yapılıyor çünkü bu fonksiyonun içinde network işlemleri yapılıyor ve network işlemleri uzun sürebilir. 
    {
        GameManager = new ClientGameManager();
        //GameManager = gameObject.AddComponent<ClientGameManager>();

        return await GameManager.InitAsync();//neden await? çünkü InitAsync fonksiyonu async bir fonksiyon.
        //InitAsync() fonksiyonu, asenkron olarak çalışabilecek işlemleri içeriyor. Örneğin, ağ bağlantıları veya başka uzun sürebilecek işlemler (sunucuya bağlanmak gibi).
        //await anahtar kelimesi, bu asenkron işlemin tamamlanmasını bekler. Böylece, fonksiyonun geri kalanının tamamlanması için bu asenkron işlemin sonuçlanmasını sağlar. 
        //Ağ işlemleri uzun sürebilir veya başka bir nedenden ötürü hemen tamamlanamayabilir, bu nedenle bekleme gereklidir.

    }

    private void OnDestroy()
    {
        GameManager?.Dispose();
    }

}
